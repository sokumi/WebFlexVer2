using Microsoft.Extensions.Options;
using WebFlex.OpcCollector.Options;

namespace WebFlex.OpcCollector.Services;

public class OpcRuntimeManager {
    private readonly OpcCollectTargetProvider _targetProvider;
    private readonly OpcUaRuntimeService _opcUaRuntimeService;
    private readonly TimescaleDbWriter _timescaleDbWriter;
    private readonly OpcCollectorOptions _options;
    private readonly ILogger<OpcRuntimeManager> _logger;
    private DateTime _lastSaveAt = DateTime.MinValue;
    private DateTime _lastWriterLogAt = DateTime.MinValue;

    private DateTime _lastReloadAt = DateTime.MinValue;

    public OpcRuntimeManager(
        OpcCollectTargetProvider targetProvider,
        OpcUaRuntimeService opcUaRuntimeService,
        TimescaleDbWriter timescaleDbWriter,
        IOptions<OpcCollectorOptions> options,
        ILogger<OpcRuntimeManager> logger) {
        _targetProvider = targetProvider;
        _opcUaRuntimeService = opcUaRuntimeService;
        _timescaleDbWriter = timescaleDbWriter;
        _options = options.Value;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken) {
        _logger.LogInformation("OPC Runtime Manager started.");

        await ReloadTargetsAsync(cancellationToken);
    }

    public async Task TickAsync(CancellationToken cancellationToken) {
        var nowUtc = DateTime.UtcNow;

        if ((nowUtc - _lastReloadAt).TotalSeconds >= _options.ReloadIntervalSeconds) {
            await ReloadTargetsAsync(cancellationToken);
        }

        if ((nowUtc - _lastSaveAt).TotalMilliseconds >= _options.SaveIntervalMilliseconds) {
            var count = _opcUaRuntimeService.EnqueueCurrentValuesSnapshot();
            _lastSaveAt = nowUtc;

            if (count > 0) {
                _logger.LogInformation(
                    "현재값 Snapshot 저장 요청 | Count={Count}",
                    count);
            }
        }

        LogWriterStatus();
    }

    public async Task StopAsync(CancellationToken cancellationToken) {
        _logger.LogInformation("OPC Runtime Manager stopping.");

        await _opcUaRuntimeService.StopAllAsync();

        _logger.LogInformation("OPC Runtime Manager stopped.");
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
        var now = DateTime.UtcNow;

        if ((now - _lastWriterLogAt).TotalSeconds < 30)
            return;

        _lastWriterLogAt = now;

        _logger.LogInformation(
            "DB Writer 상태 | Queue={QueueCount} | Enqueued={Enqueued} | Inserted={Inserted}",
            _timescaleDbWriter.QueueCount,
            _timescaleDbWriter.TotalEnqueuedCount,
            _timescaleDbWriter.TotalInsertedCount);
    }
}