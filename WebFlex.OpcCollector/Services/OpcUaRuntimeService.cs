using System.Collections.Concurrent;
using Opc.Ua;
using Opc.Ua.Client;
using WebFlex.OpcCollector.Runtime;
using WebFlex.Shared.Dtos.Opc;

namespace WebFlex.OpcCollector.Services;

public class OpcUaRuntimeService {
    private readonly ConcurrentDictionary<long, OpcDeviceRuntime> _devices = new();
    private readonly OpcUaSessionFactory _sessionFactory;
    private readonly TimescaleDbWriter _timescaleDbWriter;
    private readonly ILogger<OpcUaRuntimeService> _logger;
    private readonly OpcRuntimeStatusService _runtimeStatusService;

    public OpcUaRuntimeService(
        OpcUaSessionFactory sessionFactory,
        TimescaleDbWriter timescaleDbWriter,
        OpcRuntimeStatusService runtimeStatusService,
        ILogger<OpcUaRuntimeService> logger) {
        _sessionFactory = sessionFactory;
        _timescaleDbWriter = timescaleDbWriter;
        _runtimeStatusService = runtimeStatusService;
        _logger = logger;
    }

    public async Task SyncTargetsAsync(
        List<OpcCollectTargetDto> targets,
        CancellationToken cancellationToken) {
        var targetDeviceIds = targets.Select(x => x.DeviceId).ToHashSet();

        foreach (var runtime in _devices.Values.ToList()) {
            if (!targetDeviceIds.Contains(runtime.DeviceId)) {
                await RemoveDeviceAsync(runtime.DeviceId);
            }
        }

        foreach (var target in targets) {
            await SyncDeviceAsync(target, cancellationToken);
        }
    }

    public async Task StopAllAsync() {
        foreach (var runtime in _devices.Values.ToList()) {
            await RemoveDeviceAsync(runtime.DeviceId);
        }
    }

    private async Task SyncDeviceAsync(
        OpcCollectTargetDto target,
        CancellationToken cancellationToken) {
        OpcDeviceRuntime runtime;

        try {
            runtime = await GetOrCreateRuntimeAsync(target, cancellationToken);
        } catch (Exception ex) {
            await _runtimeStatusService.UpsertErrorAsync(
                target.DeviceId,
                target.EndpointUrl,
                ex.Message,
                cancellationToken);

            _logger.LogError(
                ex,
                "OPC 디바이스 연결 실패 | Device={DeviceName} | Endpoint={EndpointUrl}",
                target.DeviceName,
                target.EndpointUrl);

            return;
        }

        await runtime.SyncLock.WaitAsync(cancellationToken);

        try {
            var targetNodeIds = target.Tags
                .Select(x => x.NodeId)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var existingNodeId in runtime.Items.Keys.ToList()) {
                if (!targetNodeIds.Contains(existingNodeId)) {
                    RemoveMonitoredItem(runtime, existingNodeId);
                }
            }

            var addedCount = 0;

            foreach (var tag in target.Tags) {
                if (string.IsNullOrWhiteSpace(tag.NodeId))
                    continue;

                if (runtime.Items.ContainsKey(tag.NodeId)) {
                    runtime.Tags[tag.NodeId] = tag;
                    continue;
                }

                if (!TryCreateMonitoredItem(runtime, tag, out var item)) {
                    continue;
                }

                // 초기값 읽기 제거 — 구독 후 KEPServer가 값을 바로 밀어줌
                runtime.Subscription.AddItem(item);
                runtime.Items[tag.NodeId] = item;
                runtime.Tags[tag.NodeId] = tag;
                addedCount++;
            }

            if (addedCount > 0) {
                await runtime.Subscription.ApplyChangesAsync();

            }

            _logger.LogInformation(
                "OPC 디바이스 동기화 완료 | Device={DeviceName} | Endpoint={EndpointUrl} | SubscribedCount={SubscribedCount}",
                runtime.DeviceName,
                runtime.EndpointUrl,
                runtime.Items.Count);
        } finally {
            runtime.SyncLock.Release();
        }
    }

    private async Task<OpcDeviceRuntime> GetOrCreateRuntimeAsync(
        OpcCollectTargetDto target,
        CancellationToken cancellationToken) {
        if (_devices.TryGetValue(target.DeviceId, out var existing) &&
            existing.Session != null &&
            existing.Session.Connected) {
            return existing;
        }

        if (existing != null) {
            await RemoveDeviceAsync(existing.DeviceId);
        }

        var session = await _sessionFactory.CreateSessionAsync(target, cancellationToken);

        var subscription = new Subscription(session.DefaultSubscription) {
            PublishingInterval = target.PublishingIntervalMs,
            KeepAliveCount = uint.MaxValue,
            LifetimeCount = uint.MaxValue,
            MaxNotificationsPerPublish = uint.MaxValue,
            Priority = 100
        };

        session.AddSubscription(subscription);
        await subscription.CreateAsync();

        var runtime = new OpcDeviceRuntime {
            DeviceId = target.DeviceId,
            DeviceCode = target.DeviceCode,
            DeviceName = target.DeviceName,
            EndpointUrl = target.EndpointUrl,
            Session = session,
            Subscription = subscription,
            LastConnectionOkTime = DateTime.UtcNow
        };

        _devices[target.DeviceId] = runtime;

        await _runtimeStatusService.UpsertConnectedAsync(
    target.DeviceId,
    target.EndpointUrl,
    0,
    cancellationToken);

        _logger.LogInformation(
            "OPC 디바이스 연결 생성 | Device={DeviceName} | Endpoint={EndpointUrl}",
            target.DeviceName,
            target.EndpointUrl);

        return runtime;
    }

    private async Task ReadInitialValueAsync(
        OpcDeviceRuntime runtime,
        OpcCollectTargetTagDto tag,
        CancellationToken cancellationToken) {
        try {
            var nodeId = NodeId.Parse(tag.NodeId);
            var value = await runtime.Session.ReadValueAsync(nodeId);

            var nowUtc = DateTime.UtcNow;

            // Bad여도 CurrentValues에 넣음 — 구독 후 정상값으로 업데이트됨
            runtime.CurrentValues[tag.NodeId] = new OpcCurrentRuntimeValue {
                EndpointUrl = runtime.EndpointUrl,
                NodeId = tag.NodeId,
                Value = value.Value?.ToString(),
                Status = value.StatusCode.ToString(),
                SourceTimestamp = value.SourceTimestamp == DateTime.MinValue
                    ? null
                    : DateTime.SpecifyKind(value.SourceTimestamp, DateTimeKind.Utc),
                ReceivedAt = nowUtc,
                SaveToDatabase = tag.SaveToDatabase
            };

            if (StatusCode.IsGood(value.StatusCode)) {
                runtime.LastConnectionOkTime = nowUtc;
            } else {
                // Bad는 오류가 아니라 정상 상황 — 채널 오프라인 등
                _logger.LogDebug(
                    "OPC 초기값 Bad | Device={DeviceName} | NodeId={NodeId} | Status={Status}",
                    runtime.DeviceName,
                    tag.NodeId,
                    value.StatusCode);
            }
        } catch (Exception ex) {
            // ReadValueAsync가 Bad 상태를 예외로 던지는 경우 — CurrentValues에 Bad로 넣어둠
            _logger.LogDebug(
                "OPC 초기값 읽기 예외 | Device={DeviceName} | NodeId={NodeId} | {Message}",
                runtime.DeviceName,
                tag.NodeId,
                ex.Message);

            var nowUtc = DateTime.UtcNow;

            runtime.CurrentValues[tag.NodeId] = new OpcCurrentRuntimeValue {
                EndpointUrl = runtime.EndpointUrl,
                NodeId = tag.NodeId,
                Value = null,
                Status = "Bad",
                SourceTimestamp = null,
                ReceivedAt = nowUtc,
                SaveToDatabase = tag.SaveToDatabase
            };
        }
    }

    private bool TryCreateMonitoredItem(
        OpcDeviceRuntime runtime,
        OpcCollectTargetTagDto tag,
        out MonitoredItem item) {
        item = null!;

        NodeId nodeId;

        try {
            nodeId = NodeId.Parse(tag.NodeId);
        } catch (Exception ex) {
            _logger.LogError(
                ex,
                "Invalid NodeId | Device={DeviceName} | NodeId={NodeId}",
                runtime.DeviceName,
                tag.NodeId);
            return false;
        }

        item = new MonitoredItem(runtime.Subscription.DefaultItem) {
            StartNodeId = nodeId,
            AttributeId = Attributes.Value,
            DisplayName = tag.NodeId,
            SamplingInterval = tag.SamplingIntervalMs,
            QueueSize = (uint)Math.Max(1, tag.QueueSize),
            DiscardOldest = true
        };

        item.Notification += (monitoredItem, e) =>
            OnMonitoredItemNotification(runtime, monitoredItem, e);

        return true;
    }

    private void RemoveMonitoredItem(OpcDeviceRuntime runtime, string nodeId) {
        if (!runtime.Items.TryRemove(nodeId, out var item))
            return;

        runtime.Tags.TryRemove(nodeId, out _);

        try {
            runtime.Subscription.RemoveItem(item);
            item.Notification -= (monitoredItem, e) =>
                OnMonitoredItemNotification(runtime, monitoredItem, e);
        } catch (Exception ex) {
            _logger.LogError(
                ex,
                "OPC 구독 항목 제거 실패 | Device={DeviceName} | NodeId={NodeId}",
                runtime.DeviceName,
                nodeId);
        }
    }

    private void OnMonitoredItemNotification(
        OpcDeviceRuntime runtime,
        MonitoredItem item,
        MonitoredItemNotificationEventArgs e) {
        foreach (var value in item.DequeueValues()) {
            var nodeIdText = item.DisplayName;
            var nowUtc = DateTime.UtcNow;

            runtime.LastConnectionOkTime = nowUtc;

            var saveToDatabase = true;

            if (runtime.Tags.TryGetValue(nodeIdText, out var tag)) {
                saveToDatabase = tag.SaveToDatabase;
            }

            runtime.CurrentValues[nodeIdText] = new OpcCurrentRuntimeValue {
                EndpointUrl = runtime.EndpointUrl,
                NodeId = nodeIdText,
                Value = value.Value?.ToString(),
                Status = value.StatusCode.ToString(),
                SourceTimestamp = value.SourceTimestamp == DateTime.MinValue
                    ? null
                    : DateTime.SpecifyKind(value.SourceTimestamp, DateTimeKind.Utc),
                ReceivedAt = nowUtc,
                SaveToDatabase = saveToDatabase
            };

            _ = Task.Run(async () => {
                try {
                    await _runtimeStatusService.UpsertReceivedAsync(
                        runtime.DeviceId,
                        runtime.EndpointUrl,
                        runtime.Items.Count);
                } catch (Exception ex) {
                    _logger.LogError(
                        ex,
                        "OPC 수신 상태 저장 실패 | Device={DeviceName} | Endpoint={EndpointUrl}",
                        runtime.DeviceName,
                        runtime.EndpointUrl);
                }
            });

        }
    }

    private async Task RemoveDeviceAsync(long deviceId) {
        if (!_devices.TryRemove(deviceId, out var runtime))
            return;

        await runtime.SyncLock.WaitAsync();

        try {
            try {
                await runtime.Subscription.DeleteAsync(true);
                runtime.Subscription.Dispose();
            } catch {
                // ignore
            }

            try {
                await runtime.Session.CloseAsync();
                runtime.Session.Dispose();
            } catch {
                // ignore
            }

            runtime.Items.Clear();
            runtime.Tags.Clear();
            runtime.CurrentValues.Clear();

            await _runtimeStatusService.UpsertDisconnectedAsync(
                runtime.DeviceId,
                runtime.EndpointUrl);

            _logger.LogInformation(
                "OPC 디바이스 연결 정리 | Device={DeviceName} | Endpoint={EndpointUrl}",
                runtime.DeviceName,
                runtime.EndpointUrl);
        } finally {
            runtime.SyncLock.Release();
            runtime.SyncLock.Dispose();
        }
    }

    public int EnqueueCurrentValuesSnapshot() {
        var snapshotTime = DateTime.UtcNow;
        var count = 0;

        foreach (var runtime in _devices.Values) {
            if (runtime.Session == null || !runtime.Session.Connected)
                continue;

            foreach (var value in runtime.CurrentValues.Values) {
                if (!value.SaveToDatabase)
                    continue;

                _timescaleDbWriter.Enqueue(new OpcCollectedValue {
                    Time = snapshotTime,
                    EndpointUrl = value.EndpointUrl,
                    NodeId = value.NodeId,
                    Value = value.Value,
                    Status = value.Status,
                    SourceTimestamp = value.SourceTimestamp,
                    ReceivedAt = snapshotTime
                });

                count++;
            }
        }


        return count;
    }
}