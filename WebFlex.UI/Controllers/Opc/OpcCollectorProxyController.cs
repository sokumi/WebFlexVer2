using Microsoft.AspNetCore.Mvc;

namespace WebFlex.UI.Controllers.Opc;

[Route("api/opc-collector/[action]")]
public class OpcCollectorProxyController : Controller {
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OpcCollectorProxyController> _logger;

    public OpcCollectorProxyController(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<OpcCollectorProxyController> logger) {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpGet]
    public Task<IActionResult> Status(CancellationToken cancellationToken) {
        return ForwardAsync(HttpMethod.Get, "api/opc-collector/status", cancellationToken);
    }

    [HttpGet]
    public Task<IActionResult> Logs(CancellationToken cancellationToken) {
        return ForwardAsync(HttpMethod.Get, "api/opc-collector/logs?count=100", cancellationToken);
    }

    [HttpPost]
    public Task<IActionResult> RestartDevice(long deviceId, CancellationToken cancellationToken) {
        return ForwardAsync(HttpMethod.Post, $"api/opc-collector/device/{deviceId}/restart", cancellationToken);
    }

    [HttpPost]
    public Task<IActionResult> RestartAllDevices(CancellationToken cancellationToken) {
        return ForwardAsync(HttpMethod.Post, "api/opc-collector/devices/restart", cancellationToken);
    }

    [HttpPost]
    public Task<IActionResult> StopSubscription(CancellationToken cancellationToken) {
        return ForwardAsync(HttpMethod.Post, "api/opc-collector/subscription/stop", cancellationToken);
    }

    [HttpPost]
    public Task<IActionResult> StartSubscription(CancellationToken cancellationToken) {
        return ForwardAsync(HttpMethod.Post, "api/opc-collector/subscription/start", cancellationToken);
    }

    [HttpPost]
    public Task<IActionResult> StopDbSave(CancellationToken cancellationToken) {
        return ForwardAsync(HttpMethod.Post, "api/opc-collector/db-save/stop", cancellationToken);
    }

    [HttpPost]
    public Task<IActionResult> StartDbSave(CancellationToken cancellationToken) {
        return ForwardAsync(HttpMethod.Post, "api/opc-collector/db-save/start", cancellationToken);
    }

    [HttpPost]
    public Task<IActionResult> RestartProcess(CancellationToken cancellationToken) {
        return ForwardAsync(HttpMethod.Post, "api/opc-collector/restart-process", cancellationToken);
    }

    private async Task<IActionResult> ForwardAsync(
        HttpMethod method,
        string path,
        CancellationToken cancellationToken) {
        try {
            var baseUrl = _configuration["OpcCollector:BaseUrl"];

            if (string.IsNullOrWhiteSpace(baseUrl)) {
                return StatusCode(500, new {
                    success = false,
                    message = "OpcCollector:BaseUrl 설정이 없습니다."
                });
            }

            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");

            using var request = new HttpRequestMessage(method, path);
            using var response = await client.SendAsync(request, cancellationToken);

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/json";

            return new ContentResult {
                StatusCode = (int)response.StatusCode,
                Content = body,
                ContentType = contentType
            };
        } catch (Exception ex) {
            _logger.LogError(ex, "OPC Collector Proxy 호출 실패 | Path={Path}", path);

            return StatusCode(500, new {
                success = false,
                message = ex.Message
            });
        }
    }
}