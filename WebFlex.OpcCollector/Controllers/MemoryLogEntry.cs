using Microsoft.AspNetCore.Mvc;
using WebFlex.OpcCollector.Logging;
using WebFlex.OpcCollector.Services;

namespace WebFlex.OpcCollector.Controllers;

[ApiController]
[Route("api/opc-collector")]
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

    [HttpGet("logs")]
    public IActionResult Logs(int count = 100) {
        return Ok(MemoryLogStore.GetLatest(count));
    }

    [HttpPost("device/{deviceId:long}/restart")]
    public async Task<IActionResult> RestartDevice(long deviceId, CancellationToken cancellationToken) {
        await _runtimeManager.RestartDeviceAsync(deviceId, cancellationToken);
        return Ok(new { success = true, message = "디바이스 세션 종료 후 재구독 요청 완료" });
    }

    [HttpPost("devices/restart")]
    public async Task<IActionResult> RestartAllDevices(CancellationToken cancellationToken) {
        await _runtimeManager.RestartAllDevicesAsync(cancellationToken);
        return Ok(new { success = true, message = "전체 디바이스 세션 종료 후 재구독 요청 완료" });
    }

    [HttpPost("subscription/stop")]
    public async Task<IActionResult> StopSubscription(CancellationToken cancellationToken) {
        await _runtimeManager.StopSubscriptionAsync(cancellationToken);
        return Ok(new { success = true, message = "구독 중지 요청 완료" });
    }

    [HttpPost("subscription/start")]
    public async Task<IActionResult> StartSubscription(CancellationToken cancellationToken) {
        await _runtimeManager.StartSubscriptionAsync(cancellationToken);
        return Ok(new { success = true, message = "구독 재시작 요청 완료" });
    }

    [HttpPost("db-save/stop")]
    public IActionResult StopDbSave() {
        _runtimeManager.StopDbSave();
        return Ok(new { success = true, message = "DB 저장 중지 요청 완료" });
    }

    [HttpPost("db-save/start")]
    public IActionResult StartDbSave() {
        _runtimeManager.StartDbSave();
        return Ok(new { success = true, message = "DB 저장 재시작 요청 완료" });
    }

    [HttpPost("restart-process")]
    public IActionResult RestartProcess() {
        _logger.LogWarning("OPC Collector 전체 재가동 요청 수신. Windows Service Recovery 설정으로 재시작되어야 합니다.");

        Task.Run(async () => {
            await Task.Delay(1000);
            _lifetime.StopApplication();
        });

        return Ok(new { success = true, message = "전체 재가동 요청 완료" });
    }
}