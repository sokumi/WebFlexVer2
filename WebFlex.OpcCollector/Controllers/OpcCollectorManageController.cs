using Microsoft.AspNetCore.Mvc;
using WebFlex.OpcCollector.Logging;
using WebFlex.OpcCollector.Services;

namespace WebFlex.OpcCollector.Controllers;

[ApiController]
[Route("api/opc-collector-manage-manage")]
public class OpcCollectorManageController : ControllerBase {
    private readonly OpcRuntimeManager _runtimeManager;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<OpcCollectorManageController> _logger;

    public OpcCollectorManageController(
        OpcRuntimeManager runtimeManager,
        IHostApplicationLifetime lifetime,
        ILogger<OpcCollectorManageController> logger) {
        _runtimeManager = runtimeManager;
        _lifetime = lifetime;
        _logger = logger;
    }

    [HttpGet("status")]
    public IActionResult Status() {
        return Ok(_runtimeManager.GetStatus());
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

    [HttpPost("devices/restart")]
    public async Task<IActionResult> RestartAllDevices(CancellationToken cancellationToken) {
        await _runtimeManager.RestartAllDevicesAsync(cancellationToken);
        return Ok(new { success = true, message = "전체 디바이스 재구독 요청 완료" });
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

    [HttpPost("restart-process")]
    public IActionResult RestartProcess() {
        _logger.LogWarning("OPC Collector 전체 재가동 요청 수신");

        Task.Run(async () => {
            await Task.Delay(1000);
            _lifetime.StopApplication();
        });

        return Ok(new { success = true, message = "전체 재가동 요청 완료" });
    }

    [HttpGet("device-summary")]
    public async Task<IActionResult> DeviceSummary(
    CancellationToken cancellationToken) {
        var data = await _runtimeManager.GetDeviceSummaryAsync(
            cancellationToken);

        return Ok(data);
    }

}