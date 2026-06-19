using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using WebFlex.OpcCollector.Options;
using WebFlex.Shared.Dtos.Opc;

namespace WebFlex.OpcCollector.Services;

public class OpcRuntimeManager {
    private readonly OpcCollectTargetProvider _targetProvider;
    private readonly OpcUaRuntimeService _opcUaRuntimeService;
    private readonly TimescaleDbWriter _timescaleDbWriter;
    private readonly OpcCollectorOptionState _optionState;
    private readonly ILogger<OpcRuntimeManager> _logger;

    private DateTime _lastSaveAt = DateTime.MinValue;
    private DateTime _lastWriterLogAt = DateTime.MinValue;
    private DateTime _lastReloadAt = DateTime.MinValue;

    private volatile bool _subscriptionStopped;
    private volatile bool _dbSaveStopped;

    public OpcRuntimeManager(
        OpcCollectTargetProvider targetProvider,
        OpcUaRuntimeService opcUaRuntimeService,
        TimescaleDbWriter timescaleDbWriter,
        OpcCollectorOptionState optionState,
        ILogger<OpcRuntimeManager> logger) {
        _targetProvider = targetProvider;
        _opcUaRuntimeService = opcUaRuntimeService;
        _timescaleDbWriter = timescaleDbWriter;
        _optionState = optionState;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken) {
        _logger.LogInformation("OPC Runtime Manager started.");

        await ReloadTargetsAsync(cancellationToken);
    }

    public async Task TickAsync(CancellationToken cancellationToken) {
        if (_subscriptionStopped) {
            LogWriterStatus();
            return;
        }

        var options = _optionState.Current;
        var nowUtc = DateTime.UtcNow;

        if (options.EnableAutoReload &&
            (nowUtc - _lastReloadAt).TotalSeconds >= options.ReloadIntervalSeconds) {
            await ReloadTargetsAsync(cancellationToken);
        }

        if (options.EnableSnapshotSave && !_dbSaveStopped) {
            var nowSecond = new DateTime(
                nowUtc.Year, nowUtc.Month, nowUtc.Day,
                nowUtc.Hour, nowUtc.Minute, nowUtc.Second,
                DateTimeKind.Utc);

            if (nowSecond != _lastSaveAt) {
                var count = _opcUaRuntimeService.EnqueueCurrentValuesSnapshot();
                _lastSaveAt = nowSecond;

                if (count > 0) {
                    _logger.LogInformation(
                        "현재값 Snapshot 저장 요청 | Count={Count}",
                        count);
                }
            }
        }

        LogWriterStatus();
    }

    public async Task StopAsync(CancellationToken cancellationToken) {
        _logger.LogInformation("OPC Runtime Manager stopping.");

        await _opcUaRuntimeService.StopAllAsync();

        _logger.LogInformation("OPC Runtime Manager stopped.");
    }

    public async Task RestartAllDevicesAsync(CancellationToken cancellationToken) {
        _logger.LogWarning("전체 OPC 디바이스 재구독 요청");

        _subscriptionStopped = false;

        await _opcUaRuntimeService.StopAllAsync();
        await ReloadTargetsAsync(cancellationToken);

        _logger.LogInformation("전체 OPC 디바이스 재구독 완료");
    }

    public async Task StopSubscriptionAsync(CancellationToken cancellationToken) {
        _logger.LogWarning("전체 OPC 구독 중지 요청");

        _subscriptionStopped = true;
        await _opcUaRuntimeService.StopAllAsync();

        _logger.LogInformation("전체 OPC 구독 중지 완료");
    }

    public async Task StartSubscriptionAsync(CancellationToken cancellationToken) {
        _logger.LogWarning("전체 OPC 구독 재시작 요청");

        _subscriptionStopped = false;
        await ReloadTargetsAsync(cancellationToken);

        _logger.LogInformation("전체 OPC 구독 재시작 완료");
    }

    public async Task StopDeviceSubscriptionAsync(string deviceId, CancellationToken cancellationToken) {
        _logger.LogWarning("선택 디바이스 구독 중지 요청 | DeviceId={DeviceId}", deviceId);

        await _opcUaRuntimeService.StopDeviceSubscriptionAsync(deviceId);

        _logger.LogInformation("선택 디바이스 구독 중지 완료 | DeviceId={DeviceId}", deviceId);
    }

    public async Task StartDeviceSubscriptionAsync(string deviceId, CancellationToken cancellationToken) {
        _logger.LogWarning("선택 디바이스 구독 재시작 요청 | DeviceId={DeviceId}", deviceId);

        var targets = await _targetProvider.GetCollectTargetsAsync(cancellationToken);
        var target = targets.FirstOrDefault(x => x.DeviceId == deviceId);

        if (target == null) {
            _logger.LogWarning("선택 디바이스 구독 재시작 실패 - 대상 없음 | DeviceId={DeviceId}", deviceId);
            return;
        }

        await _opcUaRuntimeService.StartDeviceSubscriptionAsync(target, cancellationToken);

        _logger.LogInformation("선택 디바이스 구독 재시작 완료 | DeviceId={DeviceId}", deviceId);
    }

    public object GetStatus() {
        return new {
            subscriptionStopped = _subscriptionStopped,
            dbSaveStopped = _dbSaveStopped,
            deviceCount = _opcUaRuntimeService.DeviceCount,
            subscribedCount = _opcUaRuntimeService.SubscribedCount,
            queueCount = _timescaleDbWriter.QueueCount,
            totalEnqueued = _timescaleDbWriter.TotalEnqueuedCount,
            totalInserted = _timescaleDbWriter.TotalInsertedCount
        };
    }

    public OpcCollectorRuntimeOptionsDto GetOptions() {
        return _optionState.Current;
    }

    public OpcCollectorRuntimeOptionsDto UpdateOptions(OpcCollectorRuntimeOptionsDto request) {
        var saved = _optionState.Update(request);

        _logger.LogInformation(
            "OPC Collector 옵션 변경 | ReloadIntervalSeconds={ReloadIntervalSeconds} | FlushIntervalMilliseconds={FlushIntervalMilliseconds} | MaxBatchSize={MaxBatchSize}",
            saved.ReloadIntervalSeconds,
            saved.FlushIntervalMilliseconds,
            saved.MaxBatchSize);

        return saved;
    }

    public async Task<object> GetDeviceStatusAsync(
        string deviceId,
        CancellationToken cancellationToken) {
        var targets = await _targetProvider.GetCollectTargetsAsync(cancellationToken);
        var target = targets.FirstOrDefault(x => x.DeviceId == deviceId);

        var runtimeStatus = _opcUaRuntimeService.GetDeviceStatus(deviceId);

        return new {
            deviceId,
            deviceName = target?.DeviceName ?? "",
            tagCount = target?.Tags.Count ?? 0,
            runtimeStatus,
            queueCount = _timescaleDbWriter.QueueCount,
            totalEnqueued = _timescaleDbWriter.TotalEnqueuedCount,
            totalInserted = _timescaleDbWriter.TotalInsertedCount
        };
    }

    private async Task ReloadTargetsAsync(CancellationToken cancellationToken) {
        var targets = await _targetProvider.GetCollectTargetsAsync(cancellationToken);

        _logger.LogInformation(
            "수집 설정 Reload | DeviceCount={DeviceCount} | TagCount={TagCount}",
            targets.Count,
            targets.Sum(x => x.Tags.Count));

        await _opcUaRuntimeService.SyncTargetsAsync(targets, cancellationToken);

        _lastReloadAt = DateTime.UtcNow;
    }

    private void LogWriterStatus() {
        var options = _optionState.Current;
        var now = DateTime.UtcNow;

        if ((now - _lastWriterLogAt).TotalSeconds < options.WriterLogIntervalSeconds)
            return;

        _lastWriterLogAt = now;

        _logger.LogInformation(
            "DB Writer 상태 | Queue={QueueCount} | Enqueued={Enqueued} | Inserted={Inserted}",
            _timescaleDbWriter.QueueCount,
            _timescaleDbWriter.TotalEnqueuedCount,
            _timescaleDbWriter.TotalInsertedCount);
    }

    public async Task<object> GetDeviceSummaryAsync(CancellationToken cancellationToken) {
        var targets = await _targetProvider.GetCollectTargetsAsync(cancellationToken);

        return targets.Select(target => {
            var runtimeStatus = _opcUaRuntimeService.GetDeviceStatus(target.DeviceId);

            return new {
                deviceId = target.DeviceId,
                deviceName = target.DeviceName,
                subscriptionStatus = runtimeStatus.ToString(),
                todayInsertedCount = 0
            };
        }).ToList();
    }


}