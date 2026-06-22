using Microsoft.AspNetCore.Mvc;

namespace WebFlex.UI.Controllers.Device;

[Route("opc/[action]")]
public class OpcController : Controller {
    public OpcController() {
    }

    [HttpGet, ActionName("opc1000"), ApiExplorerSettings(IgnoreApi = true)]
    public IActionResult OPC1000() {
        ViewData["Title"] = "OPC 力绢";

        return View(MVCPath.Opc.OPC1000);
    }

    [HttpGet, ActionName("opc1020"), ApiExplorerSettings(IgnoreApi = true)]
    public IActionResult OPC1020() {
        ViewData["Title"] = "OPC 可记 汲沥";

        return View(MVCPath.Opc.OPC1020);
    }

    [HttpGet, ActionName("opc1030"), ApiExplorerSettings(IgnoreApi = true)]
    public IActionResult OPC1030() {
        ViewData["Title"] = "OPC Client 可记";

        return View(MVCPath.Opc.OPC1030);
    }

    [HttpGet, ActionName("opc3000"), ApiExplorerSettings(IgnoreApi = true)]
    public IActionResult OPC3000() {
        ViewData["Title"] = "OPC History 炼雀";

        return View(MVCPath.Opc.OPC3000);
    }
}