using Microsoft.AspNetCore.Mvc;

namespace WebFlex.UI.Controllers.Options;

[Route("option/[action]")]
public class OptionController : Controller {

    [HttpGet, ActionName("opt1000"), ApiExplorerSettings(IgnoreApi = true)]
    public IActionResult OPT1000() {
        ViewData["Title"] = "카드 대시보드 옵션";
        return View(MVCPath.Options.OPT1000);
    }
}