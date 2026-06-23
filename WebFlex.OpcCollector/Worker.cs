using WebFlex.OpcCollector.Services;

namespace WebFlex.OpcCollector;

public class Worker : BackgroundService {
    private readonly ILogger<Worker> _logger;
    private readonly OpcRuntimeManager _runtimeManager;

    public Worker(
        ILogger<Worker> logger,
        OpcRuntimeManager runtimeManager) {
        _logger = logger;
        _runtimeManager = runtimeManager;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        _logger.LogInformation("WebFlex OPC Collector started.");

        await _runtimeManager.StartAsync(stoppingToken);

        // 200ms 폴링 루프 제거 → 1초 단위 스냅샷 루프로 변경
        while (!stoppingToken.IsCancellationRequested) {
            try {
                await _runtimeManager.TickAsync(stoppingToken);
            } catch (OperationCanceledException) {
                break;
            } catch (Exception ex) {
                _logger.LogError(ex, "OPC Runtime Tick 오류");
                await Task.Delay(TimeSpan.FromMilliseconds(200), stoppingToken);
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken) {
        _logger.LogInformation("WebFlex OPC Collector stopping.");

        await _runtimeManager.StopAsync(cancellationToken);

        await base.StopAsync(cancellationToken);
    }
}