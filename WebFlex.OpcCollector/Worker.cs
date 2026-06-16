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

        while (!stoppingToken.IsCancellationRequested) {
            try {
                await _runtimeManager.TickAsync(stoppingToken);
            } catch (Exception ex) {
                _logger.LogError(ex, "OPC Runtime Tick ½ÇÆÐ");
            }

            await Task.Delay(TimeSpan.FromMilliseconds(200), stoppingToken);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken) {
        _logger.LogInformation("WebFlex OPC Collector stopping.");

        await _runtimeManager.StopAsync(cancellationToken);

        await base.StopAsync(cancellationToken);
    }
}