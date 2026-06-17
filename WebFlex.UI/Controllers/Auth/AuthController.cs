using Microsoft.AspNetCore.Mvc;

namespace WebFlex.UI.Controllers.Auth;

[Route("auth/[action]")]
public class AuthController : Controller {
    [HttpGet, ActionName("login")]
    public IActionResult Login() {
        return View(MVCPath.Auth.Login);
    }

    [HttpPost, ActionName("login")]
    public IActionResult LoginPost(string userId, string password) {
        return Redirect("/");
    }

    [HttpGet, ActionName("logout")]
    public IActionResult Logout() {
        return Redirect("/auth/login");
    }
}