using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebFlex.Shared;
using WebFlex.UI.Data;
using WebFlex.UI.DTO.Common;
using WebFlex.UI.DTO.Device;
using WebFlex.UI.Services.Device;

namespace WebFlex.UI.Controllers.Device;

[Route("device/[action]")]
public class DeviceController : Controller {
    private readonly WebFlexDbContext _db;
    private readonly OpcBrowseService _opcBrowseService;

    public DeviceController(
        WebFlexDbContext db,
        OpcBrowseService opcBrowseService) {
        _db = db;
        _opcBrowseService = opcBrowseService;
    }

    [HttpGet, ActionName("dvc1000"), ApiExplorerSettings(IgnoreApi = true)]
    public IActionResult DVC1000() {
        ViewData["Title"] = "디바이스 등록";

        return View(MVCPath.Device.DVC1000);
    }

    [HttpGet, ActionName("dvc1010"), ApiExplorerSettings(IgnoreApi = true)]
    public IActionResult DVC1010() {
        ViewData["Title"] = "디바이스 태그 관리";

        return View(MVCPath.Device.DVC1010);
    }

}