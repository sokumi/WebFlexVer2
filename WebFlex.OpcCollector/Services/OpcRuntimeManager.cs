using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using WebFlex.OpcCollector.Options;
using WebFlex.Shared.Dtos.Opc;

namespace WebFlex.OpcCollector.Services;

public class OpcRuntimeManager {
    private readonly OpcCollectTargetProvider _targetProvider;
    private readonly OpcUaRuntimeService _opcUaRuntimeService;
    private readonly OpcSnapshotPersistenceService _snapshotPersistenceService;
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
        OpcSnapshotPersistenceService snapshotPersistenceService,
        OpcCollectorOptionState optionState,
        ILogger<OpcRuntimeManager> logger) {
        _targetProvider = targetProvider;
        _opcUaRuntimeService = opcUaRuntimeService;
        _snapshotPersistenceService = snapshotPersistenceService;
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
            var interval = TimeSpan.FromMilliseconds(options.SaveIntervalMilliseconds);

            if (_lastSaveAt == DateTime.MinValue || nowUtc - _lastSaveAt >= interval) {
                var snapshotTimeUtc = new DateTime(
                    nowUtc.Year, nowUtc.Month, nowUtc.Day,
                    nowUtc.Hour, nowUtc.Minute, nowUtc.Second,
                    DateTimeKind.Utc);

                var snapshot = _opcUaRuntimeService.CreateCurrentValuesSnapshot(snapshotTimeUtc);
                _lastSaveAt = nowUtc;

                if (snapshot.Count > 0) {
                    var result = await _snapshotPersistenceService.PersistSnapshotAsync(
                        snapshot,
                        options.EnableTimescaleHistorySave,
                        options.EnableCurrentValueSave,
                        cancellationToken);

                    _logger.LogInformation(
                        "현재값 Snapshot 저장 처리 | Rows={Rows} | HistoryInserted={HistoryInserted} | CurrentValueAffected={CurrentValueAffected} | Skipped={Skipped} | TotalMs={TotalMs:N0}",
                        result.RowCount,
                        result.HistoryInsertedCount,
                        result.CurrentValueAffectedCount,
                        result.Skipped,
                        result.TotalMs);
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
            isFlushRunning = _snapshotPersistenceService.IsFlushRunning,
            totalSnapshots = _snapshotPersistenceService.TotalSnapshotCount,
            totalInserted = _snapshotPersistenceService.TotalHistoryInsertedCount,
            totalCurrentValueUpdated = _snapshotPersistenceService.TotalCurrentValueUpdatedCount,
            totalSkippedSnapshots = _snapshotPersistenceService.TotalSkippedSnapshotCount,
            lastSnapshotRows = _snapshotPersistenceService.LastSnapshotRowCount,
            lastFlushMs = _snapshotPersistenceService.LastFlushTotalMs,
            lastHistoryMs = _snapshotPersistenceService.LastHistoryMs,
            lastCurrentValueMs = _snapshotPersistenceService.LastCurrentValueMs,
            lastFlushCompletedAt = _snapshotPersistenceService.LastFlushCompletedAt
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
            isFlushRunning = _snapshotPersistenceService.IsFlushRunning,
            totalSnapshots = _snapshotPersistenceService.TotalSnapshotCount,
            totalInserted = _snapshotPersistenceService.TotalHistoryInsertedCount,
            totalCurrentValueUpdated = _snapshotPersistenceService.TotalCurrentValueUpdatedCount,
            totalSkippedSnapshots = _snapshotPersistenceService.TotalSkippedSnapshotCount,
            lastSnapshotRows = _snapshotPersistenceService.LastSnapshotRowCount,
            lastFlushMs = _snapshotPersistenceService.LastFlushTotalMs
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
          "Snapshot Writer 상태 | Running={Running} | TotalSnapshots={TotalSnapshots} | Inserted={Inserted} | CurrentValueUpdated={CurrentValueUpdated} | SkippedSnapshots={SkippedSnapshots} | LastRows={LastRows} | LastFlushMs={LastFlushMs:N0} | LastHistoryMs={LastHistoryMs:N0} | LastCurrentValueMs={LastCurrentValueMs:N0}",
          _snapshotPersistenceService.IsFlushRunning,
          _snapshotPersistenceService.TotalSnapshotCount,
          _snapshotPersistenceService.TotalHistoryInsertedCount,
          _snapshotPersistenceService.TotalCurrentValueUpdatedCount,
          _snapshotPersistenceService.TotalSkippedSnapshotCount,
          _snapshotPersistenceService.LastSnapshotRowCount,
          _snapshotPersistenceService.LastFlushTotalMs,
          _snapshotPersistenceService.LastHistoryMs,
          _snapshotPersistenceService.LastCurrentValueMs);
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
