using Microsoft.AspNetCore.Mvc;
using WebFlex.UI.Common;
using WebFlex.UI.Services;

namespace WebFlex.UI.Controllers.System;

[Route("system/service/[action]")]
public class ServiceManageController : WebFlexController {
    private readonly WindowsServiceManager _serviceManager;

    public ServiceManageController(WindowsServiceManager serviceManager) {
        _serviceManager = serviceManager;
    }

    [HttpGet, ActionName("status")]
    public IActionResult Status() {
        try {
            return Success("조회되었습니다.", _serviceManager.GetStatus());
        } catch (Exception ex) {
            return ErrorData(GetErrorMessage(ex));
        }
    }

    [HttpPost, ActionName("install")]
    public async Task<IActionResult> Install() {
        try {
            var result = await _serviceManager.InstallAsync();

            return result.Success
                ? Success(result.Message, result)
                : ErrorData(result.Message, result);
        } catch (Exception ex) {
            return ErrorData(GetErrorMessage(ex));
        }
    }

    [HttpPost, ActionName("start")]
    public async Task<IActionResult> Start() {
        try {
            var result = await _serviceManager.StartAsync();

            return result.Success
                ? Success(result.Message, result)
                : ErrorData(result.Message, result);
        } catch (Exception ex) {
            return ErrorData(GetErrorMessage(ex));
        }
    }

    [HttpPost, ActionName("stop")]
    public async Task<IActionResult> Stop() {
        try {
            var result = await _serviceManager.StopAsync();

            return result.Success
                ? Success(result.Message, result)
                : ErrorData(result.Message, result);
        } catch (Exception ex) {
            return ErrorData(GetErrorMessage(ex));
        }
    }

    [HttpPost, ActionName("restart")]
    public async Task<IActionResult> Restart() {
        try {
            var result = await _serviceManager.RestartAsync();

            return result.Success
                ? Success(result.Message, result)
                : ErrorData(result.Message, result);
        } catch (Exception ex) {
            return ErrorData(GetErrorMessage(ex));
        }
    }

    [HttpPost, ActionName("uninstall")]
    public async Task<IActionResult> Uninstall() {
        try {
            var result = await _serviceManager.UninstallAsync();

            return result.Success
                ? Success(result.Message, result)
                : ErrorData(result.Message, result);
        } catch (Exception ex) {
            return ErrorData(GetErrorMessage(ex));
        }
    }
}