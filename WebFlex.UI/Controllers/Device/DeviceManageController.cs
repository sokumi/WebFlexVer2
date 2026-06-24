using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebFlex.Shared;
using WebFlex.UI.Data;
using WebFlex.UI.DTO.Common;
using WebFlex.UI.DTO.Device;
using WebFlex.UI.Services.Device;

namespace WebFlex.UI.Controllers.Device;

[Route("device/manage/[action]")]
public class DeviceManageController : Controller {
    private readonly WebFlexDbContext _db;

    public DeviceManageController(WebFlexDbContext db) {
        _db = db;
    }

    [HttpPost, ActionName("insert")]
    public async Task<IActionResult> Save([FromBody] DeviceSaveRequest request) {
        if (string.IsNullOrWhiteSpace(request.DeviceName)) {
            return Json(ApiResponse<bool>.Fail("디바이스명을 입력하세요."));
        }

        if (string.IsNullOrWhiteSpace(request.DeviceAddress)) {
            return Json(ApiResponse<bool>.Fail("디바이스 주소를 입력하세요."));
        }

        if (request.Port <= 0) {
            return Json(ApiResponse<bool>.Fail("포트를 입력하세요."));
        }

        var now = DateTime.UtcNow;

        OpcDevice entity;

        if (!string.IsNullOrWhiteSpace(request.Id)) {
            entity = await _db.Set<OpcDevice>()
                .FirstOrDefaultAsync(x => x.ID == request.Id);
        } else {
            entity = new OpcDevice {
                ID = await CreateDeviceCodeAsync(),
                CreatedAt = now
            };

            _db.Set<OpcDevice>().Add(entity);
        }

        entity.DEVICE_NAME = request.DeviceName.Trim();
        entity.DEVICE_ADDRESS = request.DeviceAddress.Trim();
        entity.PORT = request.Port;
        entity.DEVICE_TYPE = request.DeviceType;
        entity.ENDPOINT_URL = MakeEndpointUrl(request);
        entity.IS_COLLECTENABLED = request.IsCollectEnabled;

        entity.USESECURITY = request.UseSecurity;
        entity.SECURITYPOLICY = request.SecurityPolicy;
        entity.SECURITYMODE = request.SecurityMode;

        entity.USE_ANONYMOUS = request.UseAnonymous;
        entity.USER_NAME = request.UserName;
        entity.PASSWORD = request.Password;

        entity.PUBLISHINGINTERVALMS = request.PublishingIntervalMs;
        entity.SAMPLINGINTERVALMS = request.SamplingIntervalMs;

        entity.DESCRIPTION = request.Description;
        entity.IsEnabled = request.IsEnabled;
        entity.UpdatedAt = now;

        await _db.SaveChangesAsync();

        return Json(ApiResponse<bool>.Ok(true, "저장되었습니다."));
    }

    private async Task<string> CreateDeviceCodeAsync() {
        var prefix = $"DT{DateTime.Now:yyMM}";
        var lastCode = await _db.Set<OpcDevice>()
            .Where(x => x.ID.StartsWith(prefix))
            .OrderByDescending(x => x.ID)
            .Select(x => x.ID)
            .FirstOrDefaultAsync();

        var nextNo = 1;

        if (!string.IsNullOrWhiteSpace(lastCode) &&
            lastCode.Length >= prefix.Length + 4 &&
            int.TryParse(lastCode.Substring(prefix.Length), out var lastNo)) {
            nextNo = lastNo + 1;
        }

        return $"{prefix}{nextNo:D4}";
    }

    private static string MakeEndpointUrl(DeviceSaveRequest request) {
        if (!string.IsNullOrWhiteSpace(request.EndpointUrl)) {
            return request.EndpointUrl.Trim();
        }

        if (request.DeviceType.Equals("OPCUA", StringComparison.OrdinalIgnoreCase)) {
            return $"opc.tcp://{request.DeviceAddress}:{request.Port}";
        }

        return $"{request.DeviceAddress}:{request.Port}";
    }

    [HttpPost, ActionName("delete")]
    public async Task<IActionResult> Delete([FromBody] string id) {
        var entity = await _db.Set<OpcDevice>().FirstOrDefaultAsync(x => x.ID == id);

        if (entity == null) {
            return Json(ApiResponse<bool>.Fail("디바이스 정보를 찾을 수 없습니다."));
        }

        _db.Set<OpcDevice>().Remove(entity);

        await _db.SaveChangesAsync();

        return Json(ApiResponse<bool>.Ok(true, "삭제되었습니다."));
    }

    [HttpGet, ActionName("list")]
    public async Task<IActionResult> List() {
        var data = await _db.Set<OpcDevice>()
            .AsNoTracking()
            .Select(x => new DeviceDto {
                Id = x.ID,
                DeviceName = x.DEVICE_NAME,
                DeviceAddress = x.DEVICE_ADDRESS,
                Port = x.PORT,
                EndpointUrl = x.ENDPOINT_URL,
                DeviceType = x.DEVICE_TYPE,
                IsCollectEnabled = x.IS_COLLECTENABLED,
                UseSecurity = x.USESECURITY,
                SecurityPolicy = x.SECURITYPOLICY,
                SecurityMode = x.SECURITYMODE,
                UseAnonymous = x.USE_ANONYMOUS,
                UserName = x.USER_NAME,
                Password = x.PASSWORD,
                PublishingIntervalMs = x.PUBLISHINGINTERVALMS,
                SamplingIntervalMs = x.SAMPLINGINTERVALMS,
                Description = x.DESCRIPTION,
                IsEnabled = x.IsEnabled
            })
            .ToListAsync();

        return Json(ApiResponse<List<DeviceDto>>.Ok(data));
    }
}