using Microsoft.AspNetCore.Mvc;

namespace WebFlex.UI.Controllers.Test;

[Route("test/[action]")]
public class TestController : Controller {

    [HttpGet, ActionName("tst1000")]
    public IActionResult TST1000() {
        return View(MVCPath.Test.TST1000);
    }

    [HttpGet, ActionName("tst2000")]
    public IActionResult TST2000() {
        return View(MVCPath.Test.TST2000);
    }

    [HttpGet, ActionName("tst2010")]
    public IActionResult TST2010() {
        return View(MVCPath.Test.TST2010);
    }

}