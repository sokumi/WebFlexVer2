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

    private DateTime _lastSavedSecondUtc = DateTime.MinValue;
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
            // 구독 중지 상태에서는 다음 틱까지 대기
            await Task.Delay(TimeSpan.FromMilliseconds(200), cancellationToken);
            return;
        }

        var options = _optionState.Current;
        var nowUtc = DateTime.UtcNow;

        if (options.EnableAutoReload &&
            (nowUtc - _lastReloadAt).TotalSeconds >= options.ReloadIntervalSeconds) {
            await ReloadTargetsAsync(cancellationToken);
        }

        if (options.EnableSnapshotSave && !_dbSaveStopped) {
            // 다음 정각 1초까지 정밀 대기 후 스냅샷
            await WaitForNextSecondAndSaveAsync(nowUtc, cancellationToken);
        } else {
            await Task.Delay(TimeSpan.FromMilliseconds(200), cancellationToken);
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
            // queueCount 제거 (큐 폐기)
            totalRequested = _timescaleDbWriter.TotalEnqueuedCount,
            totalInserted = _timescaleDbWriter.TotalInsertedCount,
            totalFailed = _timescaleDbWriter.TotalFailedCount,
            totalCurrentValueUpdated = _timescaleDbWriter.TotalCurrentValueUpdatedCount,
            lastSaveMs = _timescaleDbWriter.LastSaveMs,
            lastSavedAt = _timescaleDbWriter.LastSavedAt
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
            totalRequested = _timescaleDbWriter.TotalEnqueuedCount,
            totalInserted = _timescaleDbWriter.TotalInsertedCount,
            totalFailed = _timescaleDbWriter.TotalFailedCount,
            totalCurrentValueUpdated = _timescaleDbWriter.TotalCurrentValueUpdatedCount,
            lastSaveMs = _timescaleDbWriter.LastSaveMs,
            lastSavedAt = _timescaleDbWriter.LastSavedAt
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

    /// <summary>
    /// 다음 정각 1초 경계까지 정밀하게 대기한 뒤 스냅샷을 찍고 DB에 저장합니다.
    /// WebFlex 방식: 큐 없이 딕셔너리 직접 스냅샷 → 즉시 저장.
    /// </summary>
    private async Task WaitForNextSecondAndSaveAsync(DateTime nowUtc, CancellationToken cancellationToken) {
        var currentSecondUtc = TruncateToSecond(nowUtc);

        if (_lastSavedSecondUtc == DateTime.MinValue) {
            _lastSavedSecondUtc = currentSecondUtc.AddSeconds(-1);
        }

        var nextDueSecondUtc = _lastSavedSecondUtc.AddSeconds(1);

        // 아직 다음 정각이 오지 않았으면 남은 시간만큼 대기
        var remaining = (nextDueSecondUtc - DateTime.UtcNow).TotalMilliseconds;
        if (remaining > 0) {
            await Task.Delay(TimeSpan.FromMilliseconds(remaining), cancellationToken);
        }

        var snapshotTimeUtc = DateTime.UtcNow;
        var snapshot = _opcUaRuntimeService.CreateCurrentValuesSnapshot(snapshotTimeUtc);
        _lastSavedSecondUtc = nextDueSecondUtc;

        if (snapshot.Values.Count == 0) {
            return;
        }

        var result = await _timescaleDbWriter.SaveSnapshotAsync(snapshot, cancellationToken);

        if (result.HasError) {
            _logger.LogWarning(
                "Snapshot 저장 실패 | DueSecond={DueSecond:O} | SnapshotTime={SnapshotTime:O} | Rows={Rows}",
                nextDueSecondUtc,
                snapshot.SnapshotTime,
                snapshot.Values.Count);
            return;
        }

        _logger.LogInformation(
            "Snapshot 저장 완료 | DueSecond={DueSecond:O} | SnapshotTime={SnapshotTime:O} | Rows={Rows} | HistoryInserted={HistoryInserted} | CurrentValueAffected={CurrentValueAffected} | TotalMs={TotalMs:N0}",
            nextDueSecondUtc,
            snapshot.SnapshotTime,
            result.RequestedRows,
            result.HistoryInsertedRows,
            result.CurrentValueAffectedRows,
            result.TotalMs);
    }

    private void LogWriterStatus() {
        var options = _optionState.Current;
        var now = DateTime.UtcNow;

        if ((now - _lastWriterLogAt).TotalSeconds < options.WriterLogIntervalSeconds)
            return;

        _lastWriterLogAt = now;

        _logger.LogInformation(
            "History Writer 상태 | Queue={QueueCount} | Requested={Requested} | Inserted={Inserted} | Failed={Failed} | CurrentValueUpdated={CurrentValueUpdated} | LastSaveMs={LastSaveMs:N0} | LastSavedAt={LastSavedAt:O}",
            _timescaleDbWriter.QueueCount,
            _timescaleDbWriter.TotalEnqueuedCount,
            _timescaleDbWriter.TotalInsertedCount,
            _timescaleDbWriter.TotalFailedCount,
            _timescaleDbWriter.TotalCurrentValueUpdatedCount,
            _timescaleDbWriter.LastSaveMs,
            _timescaleDbWriter.LastSavedAt);
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

    private static DateTime TruncateToSecond(DateTime valueUtc) {
        return new DateTime(
            valueUtc.Year,
            valueUtc.Month,
            valueUtc.Day,
            valueUtc.Hour,
            valueUtc.Minute,
            valueUtc.Second,
            DateTimeKind.Utc);
    }
}
