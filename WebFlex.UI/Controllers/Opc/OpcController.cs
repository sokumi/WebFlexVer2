using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebFlex.Shared.Entities.Opc;
using WebFlex.UI.Data;
using WebFlex.UI.DTO.Common;
using WebFlex.UI.DTO.Device;
using WebFlex.UI.Services.Device;

namespace WebFlex.UI.Controllers.Device;

[Route("opc/[action]")]
public class OpcController : Controller {

    public OpcController() {
    }

    [HttpGet, ActionName("opc1000"), ApiExplorerSettings(IgnoreApi = true)]
    public IActionResult DVC1000() {
        ViewData["Title"] = "OPC Á¦¾î";

        return View(MVCPath.Opc.OPC1000);
    }
}