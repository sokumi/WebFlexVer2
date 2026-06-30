using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebFlex.OpcCollector.Logging;
using WebFlex.OpcCollector.Services;
using WebFlex.Shared.Dtos.Opc;

namespace WebFlex.OpcCollector.Controllers;

[Authorize]
[ApiController]
[Route("api/opc-collector-manage")]
public class OpcCollectorManageController : ControllerBase {
    private readonly OpcRuntimeManager _runtimeManager;
    private readonly ILogger<OpcCollectorManageController> _logger;

    public OpcCollectorManageController(
        OpcRuntimeManager runtimeManager,
        ILogger<OpcCollectorManageController> logger) {
        _runtimeManager = runtimeManager;
        _logger = logger;
    }

    [HttpGet("status")]
    public IActionResult Status() {
        return Ok(_runtimeManager.GetStatus());
    }

    [HttpGet("options")]
    public IActionResult Options() {
        return Ok(_runtimeManager.GetOptions());
    }

    [HttpPost("options")]
    public IActionResult SaveOptions([FromBody] OpcCollectorRuntimeOptionsDto request) {
        var saved = _runtimeManager.UpdateOptions(request);
        return Ok(new {
            success = true,
            message = "OPC Collector 옵션이 저장되었습니다. 일부 옵션은 신규 세션/재구독 시 적용됩니다.",
            data = saved
        });
    }

    [HttpGet("device/{deviceId}/status")]
    public async Task<IActionResult> DeviceStatus(string deviceId, CancellationToken cancellationToken) {
        return Ok(await _runtimeManager.GetDeviceStatusAsync(deviceId, cancellationToken));
    }

    [HttpGet("logs")]
    public IActionResult Logs(int count = 100) {
        return Ok(MemoryLogStore.GetLatest(count));
    }

    [HttpPost("device/{deviceId}/subscription/stop")]
    public async Task<IActionResult> StopDeviceSubscription(string deviceId, CancellationToken cancellationToken) {
        await _runtimeManager.StopDeviceSubscriptionAsync(deviceId, cancellationToken);
        return Ok(new { success = true, message = "선택 디바이스 구독 중지 요청 완료" });
    }

    [HttpPost("device/{deviceId}/subscription/start")]
    public async Task<IActionResult> StartDeviceSubscription(string deviceId, CancellationToken cancellationToken) {
        await _runtimeManager.StartDeviceSubscriptionAsync(deviceId, cancellationToken);
        return Ok(new { success = true, message = "선택 디바이스 구독 재시작 요청 완료" });
    }

    [HttpPost("subscription/stop")]
    public async Task<IActionResult> StopSubscription(CancellationToken cancellationToken) {
        await _runtimeManager.StopSubscriptionAsync(cancellationToken);
        return Ok(new { success = true, message = "전체 디바이스 구독 중지 요청 완료" });
    }

    [HttpPost("subscription/start")]
    public async Task<IActionResult> StartSubscription(CancellationToken cancellationToken) {
        await _runtimeManager.StartSubscriptionAsync(cancellationToken);
        return Ok(new { success = true, message = "전체 디바이스 구독 재시작 요청 완료" });
    }

    [HttpGet("device-summary")]
    public async Task<IActionResult> DeviceSummary(CancellationToken cancellationToken) {
        var data = await _runtimeManager.GetDeviceSummaryAsync(cancellationToken);
        return Ok(data);
    }
}