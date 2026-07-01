using Microsoft.AspNetCore.Mvc;

namespace WebFlex.UI.Controllers;

[Route("main/[action]")]
public class MainController : Controller {
    [HttpGet, ActionName("index"), ApiExplorerSettings(IgnoreApi = true)]
    public IActionResult Index() {
        ViewData["Title"] = "메인";
        return View(MVCPath.Main.Index);
    }

    [HttpGet, ActionName("dbd2000"), ApiExplorerSettings(IgnoreApi = true)]
    public IActionResult DBD2000() {
        ViewData["Title"] = "카드 대시보드";
        return View(MVCPath.Main.DBD2000);
    }
}