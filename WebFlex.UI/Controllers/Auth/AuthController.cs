using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using WebFlex.Shared.Entities.System;
using WebFlex.UI.Data;

namespace WebFlex.UI.Controllers.Auth;

[AllowAnonymous]
[Route("auth/[action]")]
public class AuthController : Controller {
    private readonly WebFlexDbContext _db;

    public AuthController(WebFlexDbContext db) {
        _db = db;
    }

    [HttpGet, ActionName("login")]
    public IActionResult Login(string? returnUrl = null) {
        if (User.Identity?.IsAuthenticated == true) {
            return Redirect(GetSafeReturnUrl(returnUrl));
        }

        ViewData["ReturnUrl"] = returnUrl ?? "/";
        return View(MVCPath.Auth.Login);
    }

    [HttpPost, ActionName("login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LoginPost(
        string userId,
        string password,
        string? returnUrl = null) {
        userId = (userId ?? string.Empty).Trim();
        password = password ?? string.Empty;

        ViewData["ReturnUrl"] = returnUrl ?? "/";

        if (string.IsNullOrWhiteSpace(userId)) {
            ViewData["LoginError"] = "아이디를 입력해 주세요.";
            return View(MVCPath.Auth.Login);
        }

        if (string.IsNullOrWhiteSpace(password)) {
            ViewData["LoginError"] = "비밀번호를 입력해 주세요.";
            return View(MVCPath.Auth.Login);
        }

        var passwordHash = HashPassword(password);

        var user = await _db.Set<SUser>()
            .Include(x => x.UserRoles!)
                .ThenInclude(x => x.Role)
            .FirstOrDefaultAsync(x =>
                x.USER_ID == userId &&
                x.IsEnabled);

        if (user == null || !string.Equals(user.PASSWORD_HASH, passwordHash, StringComparison.OrdinalIgnoreCase)) {
            ViewData["LoginError"] = "아이디 또는 비밀번호가 올바르지 않습니다.";
            return View(MVCPath.Auth.Login);
        }

        var claims = new List<Claim> {
            new(ClaimTypes.NameIdentifier, user.ID),
            new(ClaimTypes.Name, user.USER_ID),
            new("UserUid", user.ID),
            new("UserId", user.USER_ID),
            new("UserName", user.USER_NAME),
            new("IsAdmin", user.IS_ADMIN ? "true" : "false")
        };

        var roleCodes = user.UserRoles?
            .Where(x => x.IsEnabled && x.Role != null && x.Role.IsEnabled)
            .Select(x => x.Role!.ROLE_CODE)
            .Distinct()
            .ToList() ?? new List<string>();

        foreach (var roleCode in roleCodes) {
            claims.Add(new Claim(ClaimTypes.Role, roleCode));
            claims.Add(new Claim("RoleCode", roleCode));
        }

        var identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);

        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties {
                IsPersistent = true,
                AllowRefresh = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            });

        user.LAST_LOGIN_AT = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Redirect(GetSafeReturnUrl(returnUrl));
    }

    [Authorize]
    [HttpGet, ActionName("logout")]
    public async Task<IActionResult> Logout() {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Redirect("/auth/login");
    }

    private string GetSafeReturnUrl(string? returnUrl) {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)) {
            return returnUrl;
        }

        return "/";
    }

    private static string HashPassword(string password) {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes);
    }
}