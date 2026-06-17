using Microsoft.AspNetCore.Mvc;

namespace WebFlex.UI.Controllers.Device;

[Route("device/[action]")]
public class DeviceController : Controller {
    [HttpGet, ActionName("dvc1000"), ApiExplorerSettings(IgnoreApi = true)]
    public IActionResult DVC1000() {
        ViewData["Title"] = "디바이스 관리";

        return View(MVCPath.Device.DVC1000);
    }
}