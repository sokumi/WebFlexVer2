using System.Collections.Concurrent;
using Opc.Ua;
using Opc.Ua.Client;
using WebFlex.OpcCollector.Runtime;
using WebFlex.Shared;
using WebFlex.Shared.Dtos.Opc;

namespace WebFlex.OpcCollector.Services;

public class OpcUaRuntimeService {
    private readonly ConcurrentDictionary<string, OpcDeviceRuntime> _devices = new();
    private readonly ConcurrentDictionary<string, bool> _deviceSubscriptionStopped = new();
    private readonly ConcurrentDictionary<string, bool> _deviceDbSaveStopped = new();

    // [Fix 2] 세션 생성 레이스 컨디션 방지용 디바이스별 생성 락
    // TryGetValue 확인과 Session.Create 사이에 다른 스레드가 진입해
    // 동일 디바이스에 세션을 두 개 만드는 문제를 막습니다.
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _createLocks = new();

    // [Fix 1] KeepAlive 재연결 중복 실행 방지
    // KeepAlive 실패 이벤트는 짧은 시간에 여러 번 발화할 수 있습니다.
    // 이미 재연결 Task가 실행 중인 디바이스는 건너뜁니다.
    private readonly ConcurrentDictionary<string, bool> _reconnecting = new();

    // [Fix 1] KeepAlive 핸들러에서 즉시 재연결하기 위해 마지막으로 알려진 target 정보를 보관합니다.
    // OpcRuntimeManager 의 reload 주기를 기다리지 않고 바로 재접속할 수 있습니다.
    private readonly ConcurrentDictionary<string, OpcCollectTargetDto> _lastTargets = new();

    private readonly OpcUaSessionFactory _sessionFactory;
    private readonly ILogger<OpcUaRuntimeService> _logger;
    private readonly OpcRuntimeStatusService _runtimeStatusService;
    private readonly OpcExpressionEvaluator _expressionEvaluator;
    private readonly OpcClientOptionState _optionState;

    public DateTime LastStatusUpdatedAt { get; set; } = DateTime.MinValue;

    public int DeviceCount => _devices.Count;

    public int SubscribedCount => _devices.Values.Sum(x => x.Items.Count);

    public OpcUaRuntimeService(
        OpcUaSessionFactory sessionFactory,
        OpcRuntimeStatusService runtimeStatusService,
        OpcExpressionEvaluator expressionEvaluator,
        OpcClientOptionState optionState,
        ILogger<OpcUaRuntimeService> logger) {
        _sessionFactory = sessionFactory;
        _runtimeStatusService = runtimeStatusService;
        _expressionEvaluator = expressionEvaluator;
        _optionState = optionState;
        _logger = logger;
    }

    public async Task SyncTargetsAsync(
        List<OpcCollectTargetDto> targets,
        CancellationToken cancellationToken) {
        var targetDeviceIds = targets.Select(x => x.DeviceId).ToHashSet();

        // 더 이상 대상이 아닌 디바이스 제거
        var removeDeviceTasks = _devices.Values
            .ToList()
            .Where(r => !targetDeviceIds.Contains(r.DeviceId))
            .Select(r => RemoveDeviceAsync(r.DeviceId));

        await Task.WhenAll(removeDeviceTasks);

        // [Fix 3] 디바이스별 동기화를 순차 실행 → 병렬 실행으로 변경
        // 이전: foreach + await SyncDeviceAsync (디바이스 10개 × 세션 타임아웃 = 수십 초 블로킹)
        // 변경: Task.WhenAll → 모든 디바이스를 동시에 연결 시도
        var syncTasks = targets
            .Where(t => !_deviceSubscriptionStopped.ContainsKey(t.DeviceId))
            .Select(t => SyncDeviceAsync(t, cancellationToken));

        await Task.WhenAll(syncTasks);
    }

    // [Fix 4] StopAllAsync 도 병렬로 변경
    // 이전: foreach + await RemoveDeviceAsync (디바이스마다 Subscription.Delete + Session.Close 왕복 직렬)
    // 변경: Task.WhenAll → 동시 종료
    public async Task StopAllAsync() {
        var removeTasks = _devices.Values
            .ToList()
            .Select(r => RemoveDeviceAsync(r.DeviceId));

        await Task.WhenAll(removeTasks);
    }

    public async Task StopDeviceSubscriptionAsync(string deviceId) {
        _deviceSubscriptionStopped[deviceId] = true;
        await RemoveDeviceAsync(deviceId);
    }

    public async Task StartDeviceSubscriptionAsync(
        OpcCollectTargetDto target,
        CancellationToken cancellationToken) {
        _deviceSubscriptionStopped.TryRemove(target.DeviceId, out _);
        await SyncDeviceAsync(target, cancellationToken);
    }

    public object GetDeviceStatus(string deviceId) {
        _devices.TryGetValue(deviceId, out var runtime);

        return new {
            deviceId,
            connected = runtime?.Session?.Connected ?? false,
            subscribedCount = runtime?.Items.Count ?? 0,
            currentValueCount = runtime?.CurrentValues.Count ?? 0,
            subscriptionStopped = _deviceSubscriptionStopped.ContainsKey(deviceId),
            dbSaveStopped = _deviceDbSaveStopped.ContainsKey(deviceId)
        };
    }

    private async Task SyncDeviceAsync(
        OpcCollectTargetDto target,
        CancellationToken cancellationToken) {
        // 마지막으로 알려진 target 보관 (KeepAlive 재연결 시 사용)
        _lastTargets[target.DeviceId] = target;

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

            var removedCount = 0;

            foreach (var existingNodeId in runtime.Items.Keys.ToList()) {
                if (!targetNodeIds.Contains(existingNodeId)) {
                    RemoveMonitoredItem(runtime, existingNodeId);
                    removedCount++;
                }
            }

            if (removedCount > 0) {
                await runtime.Subscription.ApplyChangesAsync();
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

    /// <summary>
    /// 이미 연결된 세션이 있으면 반환하고, 없거나 끊어져 있으면 새로 생성합니다.
    ///
    /// [Fix 2] 레이스 컨디션 방지:
    ///   TryGetValue 확인 → CreateSessionAsync 사이에는 짧은 간격이 있어
    ///   KeepAlive 콜백(Task.Run)과 SyncTargetsAsync 루프가 동시에 이 메서드에 진입하면
    ///   같은 디바이스에 세션이 두 개 생길 수 있습니다.
    ///   디바이스별 SemaphoreSlim(_createLocks)으로 한 번에 하나만 생성하도록 보호합니다.
    /// </summary>
    private async Task<OpcDeviceRuntime> GetOrCreateRuntimeAsync(
        OpcCollectTargetDto target,
        CancellationToken cancellationToken) {
        // 빠른 경로: 이미 정상 연결된 세션이 있으면 락 없이 바로 반환
        if (_devices.TryGetValue(target.DeviceId, out var existing) &&
            existing.Session is { Connected: true }) {
            return existing;
        }

        // 디바이스별 생성 락 획득
        var createLock = _createLocks.GetOrAdd(target.DeviceId, _ => new SemaphoreSlim(1, 1));
        await createLock.WaitAsync(cancellationToken);

        try {
            // 락 안에서 이중 확인 (다른 스레드가 이미 세션을 만들었을 수 있음)
            if (_devices.TryGetValue(target.DeviceId, out existing) &&
                existing.Session is { Connected: true }) {
                return existing;
            }

            // 기존 끊긴 세션 정리
            if (existing != null) {
                await RemoveDeviceAsync(existing.DeviceId);
            }

            var session = await _sessionFactory.CreateSessionAsync(target, cancellationToken);

            var options = _optionState.Current;

            var subscription = new Subscription(session.DefaultSubscription) {
                PublishingInterval = target.PublishingIntervalMs > 0
                    ? target.PublishingIntervalMs
                    : options.PublishingInterval,
                KeepAliveCount = options.KeepAliveCount,
                LifetimeCount = options.LifetimeCount,
                MaxNotificationsPerPublish = options.MaxNotificationsPerPublish,
                PublishingEnabled = options.PublishingEnabled,
                Priority = options.Priority
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

            session.KeepAlive += (s, e) => OnSessionKeepAlive(runtime, s, e);

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
        } finally {
            createLock.Release();
        }
    }

    /// <summary>
    /// OPC UA KeepAlive 콜백.
    ///
    /// [Fix 1] 이전 코드는 session.Close() 만 호출하고 종료했습니다.
    ///   _devices 에는 끊어진 runtime 이 남아있고, Session.Connected = false 상태로 고정됩니다.
    ///   다음 ReloadTargetsAsync 주기 전까지 수집이 완전히 멈추는 문제가 있었습니다.
    ///
    /// 변경 사항:
    ///   1) _reconnecting 플래그로 중복 재연결 방지 (KeepAlive 이벤트는 짧은 시간에 여러 번 발화)
    ///   2) session.Close() 후 fire-and-forget Task 로 RemoveDeviceAsync 실행
    ///   3) _lastTargets 에 보관된 target 정보로 즉시 재연결 시도
    ///      → OpcRuntimeManager 의 reload 주기(수십 초~수 분)를 기다리지 않습니다.
    /// </summary>
    private void OnSessionKeepAlive(
        OpcDeviceRuntime runtime,
        ISession session,
        KeepAliveEventArgs e) {
        if (ServiceResult.IsGood(e.Status)) {
            runtime.LastConnectionOkTime = DateTime.UtcNow;
            return;
        }

        _logger.LogWarning(
            "OPC KeepAlive 실패 | Device={DeviceName} | Endpoint={EndpointUrl} | Status={Status}",
            runtime.DeviceName,
            runtime.EndpointUrl,
            e.Status);

        // 이미 재연결 Task 가 실행 중이면 중복 진입 방지
        if (!_reconnecting.TryAdd(runtime.DeviceId, true)) {
            return;
        }

        try {
            session.Close();
        } catch {
            // 이미 끊어진 세션이므로 예외 무시
        }

        _ = Task.Run(async () => {
            try {
                // 끊어진 세션 정리
                await RemoveDeviceAsync(runtime.DeviceId);

                // 구독이 명시적으로 중지된 디바이스는 재연결하지 않음
                if (_deviceSubscriptionStopped.ContainsKey(runtime.DeviceId)) {
                    return;
                }

                // 마지막으로 알려진 target 정보로 재연결
                if (!_lastTargets.TryGetValue(runtime.DeviceId, out var lastTarget)) {
                    _logger.LogWarning(
                        "KeepAlive 재연결 실패 - 이전 target 정보 없음 | Device={DeviceName}",
                        runtime.DeviceName);
                    return;
                }

                // 재연결 전 5초 대기 (즉시 재시도 시 연속 실패 방지)
                await Task.Delay(TimeSpan.FromSeconds(5), CancellationToken.None);

                _logger.LogInformation(
                    "OPC KeepAlive 재연결 시도 | Device={DeviceName} | Endpoint={EndpointUrl}",
                    runtime.DeviceName,
                    runtime.EndpointUrl);

                await SyncDeviceAsync(lastTarget, CancellationToken.None);
            } catch (Exception ex) {
                _logger.LogError(
                    ex,
                    "OPC KeepAlive 재연결 실패 | Device={DeviceName} | Endpoint={EndpointUrl}",
                    runtime.DeviceName,
                    runtime.EndpointUrl);
            } finally {
                _reconnecting.TryRemove(runtime.DeviceId, out _);
            }
        }, CancellationToken.None);
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

        var options = _optionState.Current;

        item = new MonitoredItem(runtime.Subscription.DefaultItem) {
            StartNodeId = nodeId,
            AttributeId = ResolveAttributeId(options.AttributeId),
            DisplayName = tag.NodeId,
            MonitoringMode = ResolveMonitoringMode(options.MonitoringMode),
            SamplingInterval = tag.SamplingIntervalMs > 0
                ? tag.SamplingIntervalMs
                : options.SamplingInterval,
            QueueSize = tag.QueueSize > 0
                ? (uint)tag.QueueSize
                : options.QueueSize,
            DiscardOldest = options.DiscardOldest
        };

        var filter = CreateDataChangeFilter(options);

        if (filter != null) {
            item.Filter = filter;
        }

        MonitoredItemNotificationEventHandler handler =
            (monitoredItem, e) => OnMonitoredItemNotification(runtime, monitoredItem, e);

        item.Notification += handler;
        runtime.ItemHandlers[tag.NodeId] = handler;

        return true;
    }

    private static uint ResolveAttributeId(string value) {
        return value switch {
            "DisplayName" => Attributes.DisplayName,
            "Description" => Attributes.Description,
            "DataType" => Attributes.DataType,
            "NodeClass" => Attributes.NodeClass,
            "BrowseName" => Attributes.BrowseName,
            _ => Attributes.Value
        };
    }

    private static MonitoringMode ResolveMonitoringMode(string value) {
        return value switch {
            "Sampling" => MonitoringMode.Sampling,
            "Disabled" => MonitoringMode.Disabled,
            _ => MonitoringMode.Reporting
        };
    }

    private static DataChangeFilter? CreateDataChangeFilter(OpcClientOptionDto options) {
        var trigger = options.DataChangeTrigger switch {
            "Status" => DataChangeTrigger.Status,
            "StatusValueTimestamp" => DataChangeTrigger.StatusValueTimestamp,
            _ => DataChangeTrigger.StatusValue
        };

        var deadbandType = options.DeadbandType switch {
            "Absolute" => (uint)DeadbandType.Absolute,
            "Percent" => (uint)DeadbandType.Percent,
            _ => (uint)DeadbandType.None
        };

        if (deadbandType == (uint)DeadbandType.None && trigger == DataChangeTrigger.StatusValue) {
            return null;
        }

        return new DataChangeFilter {
            Trigger = trigger,
            DeadbandType = deadbandType,
            DeadbandValue = options.DeadbandValue
        };
    }

    private void RemoveMonitoredItem(OpcDeviceRuntime runtime, string nodeId) {
        if (!runtime.Items.TryRemove(nodeId, out var item))
            return;

        runtime.Tags.TryRemove(nodeId, out _);

        if (runtime.ItemHandlers.TryRemove(nodeId, out var handler)) {
            item.Notification -= handler;
        }

        try {
            runtime.Subscription.RemoveItem(item);
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

            if (_deviceDbSaveStopped.TryGetValue(runtime.DeviceId, out var dbStopped) && dbStopped) {
                saveToDatabase = false;
            }

            var statusType = StatusCode.IsGood(value.StatusCode)
                ? VaribaleStatusType.Good
                : VaribaleStatusType.Bad;

            var rawValue = value.Value;
            var originalValue = rawValue?.ToString();

            var cookieValue = tag == null
                ? null
                : _expressionEvaluator.Evaluate(
                    tag.TagId,
                    tag.DataType,
                    tag.Expressions,
                    rawValue
                );

            var currentValue = new OpcCurrentRuntimeValue {
                TagId = tag?.TagId ?? "",
                GroupId = tag?.GroupId,
                Value = originalValue,
                Status = (VaribaleStatusType)statusType,
                CookieValue = cookieValue,
                SourceTimestamp = value.SourceTimestamp == DateTime.MinValue
                    ? null
                    : DateTime.SpecifyKind(value.SourceTimestamp, DateTimeKind.Utc),
                ReceivedAt = nowUtc,
                SaveToDatabase = saveToDatabase
            };

            runtime.CurrentValues[nodeIdText] = currentValue;

            var now = DateTime.UtcNow;

            if ((now - runtime.LastStatusUpdatedAt).TotalSeconds >= 30) {
                runtime.LastStatusUpdatedAt = now;

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
    }

    private async Task RemoveDeviceAsync(string deviceId) {
        if (!_devices.TryRemove(deviceId, out var runtime))
            return;

        await runtime.SyncLock.WaitAsync();

        try {
            try {
                await runtime.Subscription.DeleteAsync(true);
                runtime.Subscription.Dispose();
            } catch {
            }

            try {
                await runtime.Session.CloseAsync();
                runtime.Session.Dispose();
            } catch {
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

    public OpcHistorySnapshot CreateCurrentValuesSnapshot(DateTime snapshotTimeUtc) {
        var snapshot = new OpcHistorySnapshot {
            SnapshotTime = snapshotTimeUtc
        };

        foreach (var runtime in _devices.Values) {
            if (runtime.Session == null || !runtime.Session.Connected)
                continue;

            foreach (var value in runtime.CurrentValues.Values) {
                if (!value.SaveToDatabase)
                    continue;

                snapshot.Values.Add(new OpcCollectedValue {
                    Time = snapshotTimeUtc,
                    GroupId = value.GroupId,
                    TagId = value.TagId,
                    Value = value.Value,
                    Status = value.Status,
                    CookieValue = value.CookieValue,
                    SourceTimestamp = value.SourceTimestamp,
                    ReceivedAt = value.ReceivedAt
                });
            }
        }

        return snapshot;
    }
}