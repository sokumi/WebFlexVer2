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
            await WaitForNextSecondAndSaveAsync(cancellationToken);  // ← nowUtc 제거
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
            totalSnapshotRows = _timescaleDbWriter.TotalSnapshotRows,
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
            "OPC Collector 옵션 변경 | ReloadIntervalSeconds={ReloadIntervalSeconds} | MaxBatchSize={MaxBatchSize}",
            saved.ReloadIntervalSeconds,
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
            totalSnapshotRows = _timescaleDbWriter.TotalSnapshotRows,
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
    private async Task WaitForNextSecondAndSaveAsync(CancellationToken cancellationToken) {
        var nowUtc = DateTime.UtcNow;
        var currentSecondUtc = TruncateToSecond(nowUtc);

        if (_lastSavedSecondUtc == DateTime.MinValue) {
            _lastSavedSecondUtc = currentSecondUtc.AddSeconds(-1);
        }

        var nextDueSecondUtc = _lastSavedSecondUtc.AddSeconds(1);

        // 정각(nextDue)이 완전히 지날 때까지 대기
        // 예: nextDue = 32.000 이면 32.000을 넘길 때까지 대기
        var remaining = (nextDueSecondUtc - DateTime.UtcNow).TotalMilliseconds;
        if (remaining > 0) {
            await Task.Delay(TimeSpan.FromMilliseconds(remaining), cancellationToken);
        }

        // 정각이 지났는지 한 번 더 확인 (Task.Delay 오차 보정)
        while (DateTime.UtcNow < nextDueSecondUtc) {
            await Task.Delay(1, cancellationToken);
        }

        // 스냅샷 시각을 실제 현재 시각이 아닌 dueSecond 정각으로 고정
        var snapshotTimeUtc = nextDueSecondUtc;
        var snapshot = _opcUaRuntimeService.CreateCurrentValuesSnapshot(snapshotTimeUtc);

        // 저장 성공/실패 관계없이 lastSaved 를 dueSecond 로 기록
        _lastSavedSecondUtc = nextDueSecondUtc;

        if (snapshot.Values.Count == 0) return;

        var result = await _timescaleDbWriter.SaveSnapshotAsync(snapshot, cancellationToken);

        if (result.HasError) {
            _logger.LogWarning(
                "Snapshot 저장 실패 | DueSecond={DueSecond:O} | Rows={Rows}",
                nextDueSecondUtc,
                snapshot.Values.Count);
            return;
        }

        _logger.LogInformation(
            "Snapshot 저장 완료 | DueSecond={DueSecond:O} | Rows={Rows} | TotalMs={TotalMs:N0}",
            nextDueSecondUtc,
            result.RequestedRows,
            result.TotalMs);
    }

    private void LogWriterStatus() {
        var options = _optionState.Current;
        var now = DateTime.UtcNow;

        if ((now - _lastWriterLogAt).TotalSeconds < options.WriterLogIntervalSeconds)
            return;

        _lastWriterLogAt = now;

        _logger.LogInformation(
            "History Writer 상태 | SnapshotRows={SnapshotRows} | Inserted={Inserted} | Failed={Failed} | CurrentValueUpdated={CurrentValueUpdated} | LastSaveMs={LastSaveMs:N0} | LastSavedAt={LastSavedAt:O}",
            _timescaleDbWriter.TotalSnapshotRows,
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
