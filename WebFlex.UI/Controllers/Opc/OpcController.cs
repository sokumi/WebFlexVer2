using Microsoft.AspNetCore.Mvc;

namespace WebFlex.UI.Controllers.Device;

[Route("opc/[action]")]
public class OpcController : Controller {
    public OpcController() {
    }

    [HttpGet, ActionName("opc1000"), ApiExplorerSettings(IgnoreApi = true)]
    public IActionResult OPC1000() {
        ViewData["Title"] = "OPC 제어";

        return View(MVCPath.Opc.OPC1000);
    }

    [HttpGet, ActionName("opc1020"), ApiExplorerSettings(IgnoreApi = true)]
    public IActionResult OPC1020() {
        ViewData["Title"] = "OPC 옵션 설정";

        return View(MVCPath.Opc.OPC1020);
    }
}