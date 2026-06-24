using Microsoft.AspNetCore.Mvc;

namespace WebFlex.UI.Controllers.Device;

[Route("system/[action]")]
public class SystemController : Controller {
    public SystemController() {
    }

    [HttpGet, ActionName("svc1000"), ApiExplorerSettings(IgnoreApi = true)]
    public IActionResult SVC1000() {
        ViewData["Title"] = "Windows Service Á¦¾î";

        return View(MVCPath.System.SVC1000);
    }

}