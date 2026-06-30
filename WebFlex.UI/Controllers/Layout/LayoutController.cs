using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebFlex.Shared.Entities.System;
using WebFlex.UI.Data;

namespace WebFlex.UI.Controllers.Layout;

[Authorize]
[Route("layout/[action]")]
public class LayoutController : Controller {
    private readonly WebFlexDbContext _db;

    public LayoutController(WebFlexDbContext db) {
        _db = db;
    }

    [HttpGet, ActionName("menu")]
    public async Task<IActionResult> Menu() {
        var roleCodes = User.FindAll(ClaimTypes.Role)
            .Select(x => x.Value)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .ToList();

        if (roleCodes.Count == 0) {
            return Json(new {
                success = true,
                message = "조회되었습니다.",
                data = Array.Empty<LayoutMenuDto>()
            });
        }

        var menuIds = await _db.Set<SRoleMenu>()
            .AsNoTracking()
            .Where(x =>
                x.IsEnabled &&
                x.CAN_READ &&
                roleCodes.Contains(x.ROLE_ID))
            .Select(x => x.MENU_ID)
            .Distinct()
            .ToListAsync();

        var menus = await _db.Set<SMenu>()
            .AsNoTracking()
            .Where(x =>
                x.IsEnabled &&
                x.SHOW_IN_MENU &&
                menuIds.Contains(x.ID))
            .OrderBy(x => x.SORT_ORDER ?? 9999)
            .ThenBy(x => x.MENU_NAME)
            .ToListAsync();

        var result = menus
            .Where(x => string.IsNullOrWhiteSpace(x.PARENT_MENU_ID))
            .OrderBy(x => x.SORT_ORDER ?? 9999)
            .ThenBy(x => x.MENU_NAME)
            .Select(parent => new LayoutMenuDto {
                Id = parent.ID,
                ParentId = parent.PARENT_MENU_ID,
                MenuCode = parent.MENU_CODE,
                MenuName = parent.MENU_NAME,
                Url = NormalizeUrl(parent.URL),
                Icon = NormalizeIcon(parent.ICON),
                SortOrder = parent.SORT_ORDER ?? 9999,
                Children = menus
                    .Where(child => child.PARENT_MENU_ID == parent.ID)
                    .OrderBy(child => child.SORT_ORDER ?? 9999)
                    .ThenBy(child => child.MENU_NAME)
                    .Select(child => new LayoutMenuDto {
                        Id = child.ID,
                        ParentId = child.PARENT_MENU_ID,
                        MenuCode = child.MENU_CODE,
                        MenuName = child.MENU_NAME,
                        Url = NormalizeUrl(child.URL),
                        Icon = NormalizeIcon(child.ICON),
                        SortOrder = child.SORT_ORDER ?? 9999
                    })
                    .ToList()
            })
            .ToList();

        return Json(new {
            success = true,
            message = "조회되었습니다.",
            data = result
        });
    }

    private static string NormalizeUrl(string? url) {
        if (string.IsNullOrWhiteSpace(url)) {
            return "#";
        }

        var value = url.Trim();

        if (value != "/" && value.EndsWith('/')) {
            value = value.TrimEnd('/');
        }

        return value;
    }

    private static string NormalizeIcon(string? icon) {
        return string.IsNullOrWhiteSpace(icon)
            ? "circle"
            : icon.Trim();
    }

    private class LayoutMenuDto {
        public string Id { get; set; } = "";
        public string? ParentId { get; set; }
        public string MenuCode { get; set; } = "";
        public string MenuName { get; set; } = "";
        public string Url { get; set; } = "#";
        public string Icon { get; set; } = "circle";
        public int SortOrder { get; set; }
        public List<LayoutMenuDto> Children { get; set; } = new();
    }
}