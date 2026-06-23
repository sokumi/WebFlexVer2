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

    [HttpPost, ActionName("save")]
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
            entity = await _db.OpcDevices
                .FirstOrDefaultAsync(x => x.Id == request.Id);
        } else {
            entity = new OpcDevice {
                DeviceCode = await CreateDeviceCodeAsync(),
                CreatedAt = now
            };

            _db.OpcDevices.Add(entity);
        }

        entity.DeviceName = request.DeviceName.Trim();
        entity.DeviceAddress = request.DeviceAddress.Trim();
        entity.Port = request.Port;
        entity.DeviceType = request.DeviceType;
        entity.EndpointUrl = MakeEndpointUrl(request);
        entity.IsCollectEnabled = request.IsCollectEnabled;

        entity.UseSecurity = request.UseSecurity;
        entity.SecurityPolicy = request.SecurityPolicy;
        entity.SecurityMode = request.SecurityMode;

        entity.UseAnonymous = request.UseAnonymous;
        entity.UserName = request.UserName;
        entity.Password = request.Password;

        entity.PublishingIntervalMs = request.PublishingIntervalMs;
        entity.SamplingIntervalMs = request.SamplingIntervalMs;
        entity.QueueSize = request.QueueSize;

        entity.SortOrder = request.SortOrder;
        entity.Description = request.Description;
        entity.IsEnabled = request.IsEnabled;
        entity.UpdatedAt = now;

        await _db.SaveChangesAsync();

        return Json(ApiResponse<bool>.Ok(true, "저장되었습니다."));
    }

    private async Task<string> CreateDeviceCodeAsync() {
        var prefix = $"DT{DateTime.Now:yyMM}";
        var lastCode = await _db.OpcDevices
            .Where(x => x.DeviceCode.StartsWith(prefix))
            .OrderByDescending(x => x.DeviceCode)
            .Select(x => x.DeviceCode)
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
        var entity = await _db.OpcDevices.FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null) {
            return Json(ApiResponse<bool>.Fail("디바이스 정보를 찾을 수 없습니다."));
        }

        _db.OpcDevices.Remove(entity);

        await _db.SaveChangesAsync();

        return Json(ApiResponse<bool>.Ok(true, "삭제되었습니다."));
    }

    [HttpGet, ActionName("list")]
    public async Task<IActionResult> List() {
        var data = await _db.OpcDevices
            .AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.DeviceName)
            .Select(x => new DeviceDto {
                Id = x.Id,
                DeviceCode = x.DeviceCode,
                DeviceName = x.DeviceName,
                DeviceAddress = x.DeviceAddress,
                Port = x.Port,
                EndpointUrl = x.EndpointUrl,
                DeviceType = x.DeviceType,
                IsCollectEnabled = x.IsCollectEnabled,
                UseSecurity = x.UseSecurity,
                SecurityPolicy = x.SecurityPolicy,
                SecurityMode = x.SecurityMode,
                UseAnonymous = x.UseAnonymous,
                UserName = x.UserName,
                Password = x.Password,
                PublishingIntervalMs = x.PublishingIntervalMs,
                SamplingIntervalMs = x.SamplingIntervalMs,
                QueueSize = x.QueueSize,
                SortOrder = x.SortOrder,
                Description = x.Description,
                IsEnabled = x.IsEnabled
            })
            .ToListAsync();

        return Json(ApiResponse<List<DeviceDto>>.Ok(data));
    }
}