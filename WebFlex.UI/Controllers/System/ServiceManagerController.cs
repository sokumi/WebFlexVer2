using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebFlex.UI.Services;

namespace WebFlex.UI.Controllers;

[Authorize]
[Route("system/service")]
public sealed class ServiceManagerController : Controller {
    private readonly WindowsServiceManager _serviceManager;

    public ServiceManagerController(WindowsServiceManager serviceManager) {
        _serviceManager = serviceManager;
    }

    [HttpGet("")]
    public IActionResult SVC1000() {
        return View("~/Views/System/SVC1000.cshtml");
    }

    [HttpGet("status")]
    public IActionResult Status() {
        var result = _serviceManager.GetStatus();
        return Json(result);
    }

    [HttpPost("install")]
    public async Task<IActionResult> Install() {
        var result = await _serviceManager.InstallAsync();
        return Json(result);
    }

    [HttpPost("start")]
    public async Task<IActionResult> Start() {
        var result = await _serviceManager.StartAsync();
        return Json(result);
    }

    [HttpPost("stop")]
    public async Task<IActionResult> Stop() {
        var result = await _serviceManager.StopAsync();
        return Json(result);
    }

    [HttpPost("restart")]
    public async Task<IActionResult> Restart() {
        var result = await _serviceManager.RestartAsync();
        return Json(result);
    }

    [HttpPost("uninstall")]
    public async Task<IActionResult> Uninstall() {
        var result = await _serviceManager.UninstallAsync();
        return Json(result);
    }
}