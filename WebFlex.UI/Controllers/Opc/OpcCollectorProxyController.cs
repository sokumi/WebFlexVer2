using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;

namespace WebFlex.UI.Controllers.Opc;

[Authorize]
[ApiController]
[Route("api/opc-collector")]
public class OpcCollectorProxyController : ControllerBase {
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

    [HttpGet("status")]
    public async Task<IActionResult> Status(CancellationToken cancellationToken) {
        return await ForwardAsync(HttpMethod.Get, "status", cancellationToken);
    }

    [HttpGet("device-summary")]
    public async Task<IActionResult> DeviceSummary(CancellationToken cancellationToken) {
        return await ForwardAsync(HttpMethod.Get, "device-summary", cancellationToken);
    }

    [HttpGet("logs")]
    public async Task<IActionResult> Logs(
        int count = 100,
        CancellationToken cancellationToken = default) {
        return await ForwardAsync(HttpMethod.Get, $"logs?count={count}", cancellationToken);
    }

    [HttpGet("device/{deviceId}/status")]
    public async Task<IActionResult> DeviceStatus(
        string deviceId,
        CancellationToken cancellationToken) {
        return await ForwardAsync(
            HttpMethod.Get,
            $"device/{Uri.EscapeDataString(deviceId)}/status",
            cancellationToken);
    }

    [HttpPost("subscription/stop")]
    public async Task<IActionResult> StopSubscription(CancellationToken cancellationToken) {
        return await ForwardAsync(HttpMethod.Post, "subscription/stop", cancellationToken);
    }

    [HttpPost("subscription/start")]
    public async Task<IActionResult> StartSubscription(CancellationToken cancellationToken) {
        return await ForwardAsync(HttpMethod.Post, "subscription/start", cancellationToken);
    }

    [HttpPost("device/{deviceId}/subscription/stop")]
    public async Task<IActionResult> StopDeviceSubscription(
        string deviceId,
        CancellationToken cancellationToken) {
        return await ForwardAsync(
            HttpMethod.Post,
            $"device/{Uri.EscapeDataString(deviceId)}/subscription/stop",
            cancellationToken);
    }

    [HttpPost("device/{deviceId}/subscription/start")]
    public async Task<IActionResult> StartDeviceSubscription(
        string deviceId,
        CancellationToken cancellationToken) {
        return await ForwardAsync(
            HttpMethod.Post,
            $"device/{Uri.EscapeDataString(deviceId)}/subscription/start",
            cancellationToken);
    }

    [HttpPost("devices/restart")]
    public async Task<IActionResult> RestartAllDevices(CancellationToken cancellationToken) {
        return await ForwardAsync(HttpMethod.Post, "devices/restart", cancellationToken);
    }

    [HttpPost("restart-process")]
    public async Task<IActionResult> RestartProcess(CancellationToken cancellationToken) {
        return await ForwardAsync(HttpMethod.Post, "restart-process", cancellationToken);
    }

    [HttpGet("options")]
    public async Task<IActionResult> Options(CancellationToken cancellationToken) {
        return await ForwardAsync(HttpMethod.Get, "options", cancellationToken);
    }

    [HttpPost("options")]
    public async Task<IActionResult> SaveOptions(
        [FromBody] object requestBody,
        CancellationToken cancellationToken) {
        return await ForwardJsonAsync(
            HttpMethod.Post,
            "options",
            requestBody,
            cancellationToken);
    }

    [HttpPost("history/read")]
    public async Task<IActionResult> ReadHistory(
        [FromBody] object requestBody,
        CancellationToken cancellationToken) {
        return await ForwardCollectorApiJsonAsync(
            HttpMethod.Post,
            "api/opc-history/read",
            requestBody,
            cancellationToken);
    }

    private async Task<IActionResult> ForwardAsync(
        HttpMethod method,
        string collectorPath,
        CancellationToken cancellationToken) {
        var baseUrl = _configuration["OpcCollector:BaseUrl"];

        if (string.IsNullOrWhiteSpace(baseUrl)) {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                "OpcCollector:BaseUrl 설정이 없습니다.");
        }

        var url = $"{baseUrl.TrimEnd('/')}/api/opc-collector-manage/{collectorPath.TrimStart('/')}";

        try {
            var client = _httpClientFactory.CreateClient();

            using var request = new HttpRequestMessage(method, url);
            ApplyCollectorJwt(request);

            using var response = await client.SendAsync(request, cancellationToken);

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/json";

            return new ContentResult {
                StatusCode = (int)response.StatusCode,
                Content = body,
                ContentType = contentType
            };
        } catch (Exception ex) {
            _logger.LogError(
                ex,
                "OPC Collector API 호출 실패 | Method={Method} | Url={Url}",
                method.Method,
                url);

            return StatusCode(
                StatusCodes.Status502BadGateway,
                "OPC Collector API 호출 중 오류가 발생했습니다.");
        }
    }

    private async Task<IActionResult> ForwardJsonAsync(
        HttpMethod method,
        string collectorPath,
        object requestBody,
        CancellationToken cancellationToken) {
        var baseUrl = _configuration["OpcCollector:BaseUrl"];

        if (string.IsNullOrWhiteSpace(baseUrl)) {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                "OpcCollector:BaseUrl 설정이 없습니다.");
        }

        var url = $"{baseUrl.TrimEnd('/')}/api/opc-collector-manage/{collectorPath.TrimStart('/')}";

        try {
            var client = _httpClientFactory.CreateClient();

            using var request = new HttpRequestMessage(method, url);
            request.Content = JsonContent.Create(requestBody);
            ApplyCollectorJwt(request);

            using var response = await client.SendAsync(request, cancellationToken);

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/json";

            return new ContentResult {
                StatusCode = (int)response.StatusCode,
                Content = body,
                ContentType = contentType
            };
        } catch (Exception ex) {
            _logger.LogError(
                ex,
                "OPC Collector API 호출 실패 | Method={Method} | Url={Url}",
                method.Method,
                url);

            return StatusCode(
                StatusCodes.Status502BadGateway,
                "OPC Collector API 호출 중 오류가 발생했습니다.");
        }
    }

    private async Task<IActionResult> ForwardCollectorApiJsonAsync(
        HttpMethod method,
        string apiPath,
        object requestBody,
        CancellationToken cancellationToken) {
        var baseUrl = _configuration["OpcCollector:BaseUrl"];

        if (string.IsNullOrWhiteSpace(baseUrl)) {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                "OpcCollector:BaseUrl 설정이 없습니다.");
        }

        var url = $"{baseUrl.TrimEnd('/')}/{apiPath.TrimStart('/')}";

        try {
            var client = _httpClientFactory.CreateClient();

            using var request = new HttpRequestMessage(method, url);
            request.Content = JsonContent.Create(requestBody);
            ApplyCollectorJwt(request);

            using var response = await client.SendAsync(request, cancellationToken);

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/json";

            return new ContentResult {
                StatusCode = (int)response.StatusCode,
                Content = body,
                ContentType = contentType
            };
        } catch (Exception ex) {
            _logger.LogError(
                ex,
                "OPC Collector API 호출 실패 | Method={Method} | Url={Url}",
                method.Method,
                url);

            return StatusCode(
                StatusCodes.Status502BadGateway,
                "OPC Collector API 호출 중 오류가 발생했습니다.");
        }
    }

    private void ApplyCollectorJwt(HttpRequestMessage request) {
        var token = CreateCollectorJwt();
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private string CreateCollectorJwt() {
        var issuer = _configuration["OpcCollector:Jwt:Issuer"];
        var audience = _configuration["OpcCollector:Jwt:Audience"];
        var secretKey = _configuration["OpcCollector:Jwt:SecretKey"];

        if (string.IsNullOrWhiteSpace(issuer) ||
            string.IsNullOrWhiteSpace(audience) ||
            string.IsNullOrWhiteSpace(secretKey)) {
            throw new InvalidOperationException("OpcCollector:Jwt 설정이 없습니다.");
        }

        var claims = new List<Claim> {
            new(JwtRegisteredClaimNames.Sub, User.FindFirstValue("UserUid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown"),
            new(JwtRegisteredClaimNames.UniqueName, User.Identity?.Name ?? "unknown"),
            new("UserUid", User.FindFirstValue("UserUid") ?? ""),
            new("UserId", User.FindFirstValue("UserId") ?? ""),
            new("UserName", User.FindFirstValue("UserName") ?? ""),
            new("TokenType", "OpcCollectorAccess")
        };

        foreach (var role in User.FindAll(ClaimTypes.Role).Select(x => x.Value).Distinct()) {
            claims.Add(new Claim(ClaimTypes.Role, role));
            claims.Add(new Claim("RoleCode", role));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: DateTime.UtcNow.AddSeconds(-10),
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}