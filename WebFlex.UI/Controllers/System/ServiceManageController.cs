using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebFlex.UI.Models;
using WebFlex.UI.Services;

namespace WebFlex.UI.Controllers;

//[Authorize]
[Route("system/service")]
public sealed class ServiceManagerController : Controller {
    private readonly WindowsServiceManager _serviceManager;
    private readonly ILogger<ServiceManagerController> _logger;

    public ServiceManagerController(
        WindowsServiceManager serviceManager,
        ILogger<ServiceManagerController> logger) {
        _serviceManager = serviceManager;
        _logger = logger;
    }

    [HttpGet("")]
    public IActionResult SVC1000() {
        return View("~/Views/System/SVC1000.cshtml");
    }

    [HttpGet("status")]
    public IActionResult Status() {
        try {
            var result = _serviceManager.GetStatus();
            return Json(result);
        } catch (Exception ex) {
            _logger.LogError(ex, "서비스 상태 조회 API 실패");

            return Json(new WindowsServiceStatusDto {
                ServiceName = "",
                DisplayName = "",
                Status = "Error",
                Exists = false,
                ExePath = "",
                Error = ex.ToString()
            });
        }
    }

    [HttpPost("install")]
    public async Task<IActionResult> Install() {
        try {
            var result = await _serviceManager.InstallAsync();
            return Json(result);
        } catch (Exception ex) {
            _logger.LogError(ex, "서비스 등록 API 실패");

            return Json(new WindowsServiceCommandResultDto {
                Success = false,
                Message = ex.Message,
                Error = ex.ToString()
            });
        }
    }

    [HttpPost("start")]
    public async Task<IActionResult> Start() {
        try {
            var result = await _serviceManager.StartAsync();
            return Json(result);
        } catch (Exception ex) {
            _logger.LogError(ex, "서비스 시작 API 실패");

            return Json(new WindowsServiceCommandResultDto {
                Success = false,
                Message = ex.Message,
                Error = ex.ToString()
            });
        }
    }

    [HttpPost("stop")]
    public async Task<IActionResult> Stop() {
        try {
            var result = await _serviceManager.StopAsync();
            return Json(result);
        } catch (Exception ex) {
            _logger.LogError(ex, "서비스 중지 API 실패");

            return Json(new WindowsServiceCommandResultDto {
                Success = false,
                Message = ex.Message,
                Error = ex.ToString()
            });
        }
    }

    [HttpPost("restart")]
    public async Task<IActionResult> Restart() {
        try {
            var result = await _serviceManager.RestartAsync();
            return Json(result);
        } catch (Exception ex) {
            _logger.LogError(ex, "서비스 재시작 API 실패");

            return Json(new WindowsServiceCommandResultDto {
                Success = false,
                Message = ex.Message,
                Error = ex.ToString()
            });
        }
    }

    [HttpPost("uninstall")]
    public async Task<IActionResult> Uninstall() {
        try {
            var result = await _serviceManager.UninstallAsync();
            return Json(result);
        } catch (Exception ex) {
            _logger.LogError(ex, "서비스 삭제 API 실패");

            return Json(new WindowsServiceCommandResultDto {
                Success = false,
                Message = ex.Message,
                Error = ex.ToString()
            });
        }
    }
}