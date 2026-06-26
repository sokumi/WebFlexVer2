using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebFlex.Shared;
using WebFlex.UI.Data;
using WebFlex.UI.DTO;
using WebFlex.UI.Services;

namespace WebFlex.UI.Controllers.Device;

[Route("device/manage/[action]")]
public class DeviceManageController : Controller {
    private readonly WebFlexDbContext _db;

    public DeviceManageController(WebFlexDbContext db) {
        _db = db;
    }

    [HttpGet, ActionName("summary")]
    public async Task<IActionResult> Summary() {
        var query = _db.Set<OpcDevice>().AsNoTracking();

        var totalCount = await query.CountAsync();
        var enabledCount = await query.CountAsync(x => x.IsEnabled);
        var collectCount = await query.CountAsync(x => x.IS_COLLECTENABLED);
        var tagCount = await _db.Set<OpcTag>().AsNoTracking().CountAsync();

        return Json(new {
            success = true,
            message = "조회되었습니다.",
            data = new {
                totalCount,
                enabledCount,
                collectCount,
                connectedCount = 0,
                tagCount
            }
        });
    }

    [HttpGet, ActionName("list")]
    public async Task<IActionResult> List(string? keyword = null, string? deviceType = null, bool? onlyCollect = null) {
        var query = _db.Set<OpcDevice>()
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword)) {
            query = query.Where(x =>
                x.DEVICE_NAME.Contains(keyword) ||
                (x.DEVICE_ADDRESS != null && x.DEVICE_ADDRESS.Contains(keyword)) ||
                x.ENDPOINT_URL.Contains(keyword)
            );
        }

        if (!string.IsNullOrWhiteSpace(deviceType)) {
            query = query.Where(x => x.DEVICE_TYPE == deviceType);
        }

        if (onlyCollect == true) {
            query = query.Where(x => x.IS_COLLECTENABLED);
        }

        var rows = await query
    .OrderBy(x => x.DEVICE_NAME)
    .Select(x => new {
        id = x.ID,
        deviceCode = x.ID,
        deviceName = x.DEVICE_NAME,
        deviceAddress = x.DEVICE_ADDRESS ?? "",
        port = x.PORT ?? 0,
        endpointUrl = x.ENDPOINT_URL,
        deviceType = x.DEVICE_TYPE ?? "",
        isCollectEnabled = x.IS_COLLECTENABLED,
        useSecurity = x.USESECURITY,
        securityPolicy = x.SECURITYPOLICY,
        securityMode = x.SECURITYMODE,
        useAnonymous = x.USE_ANONYMOUS,
        userName = x.USER_NAME,
        password = x.PASSWORD,
        publishingIntervalMs = x.PUBLISHINGINTERVALMS ?? 1000,
        samplingIntervalMs = x.SAMPLINGINTERVALMS ?? 1000,
        queueSize = 1,
        sortOrder = 0,
        description = x.DESCRIPTION,
        isEnabled = x.IsEnabled,
        tagCount = _db.Set<OpcTag>().Count(t => t.DEVICE_ID == x.ID)
    })
    .ToListAsync();

        return Json(new {
            success = true,
            message = "조회되었습니다.",
            data = rows
        });
    }

    [HttpGet, ActionName("types")]
    public IActionResult Types() {
        return Json(new {
            success = true,
            message = "조회되었습니다.",
            data = new[] {
                new { value = "OPCUA", text = "OPC UA" },
                new { value = "MODBUS_TCP", text = "Modbus TCP" }
            }
        });
    }

    [HttpPost, ActionName("save")]
    public async Task<IActionResult> Save([FromBody] DeviceSaveRequest request) {




        if (string.IsNullOrWhiteSpace(request.DeviceName)) {
            return Json(new {
                success = false,
                message = "디바이스명을 입력해 주세요."
            });
        }

        if (string.IsNullOrWhiteSpace(request.DeviceType)) {
            return Json(new {
                success = false,
                message = "디바이스 타입을 선택해 주세요."
            });
        }

        if (string.IsNullOrWhiteSpace(request.DeviceAddress)) {
            return Json(new {
                success = false,
                message = "주소를 입력해 주세요."
            });
        }

        if (request.Port <= 0) {
            return Json(new {
                success = false,
                message = "포트를 입력해 주세요."
            });
        }

        var now = DateTime.UtcNow;
        var endpointUrl = CreateEndpointUrl(request);

        if (string.IsNullOrWhiteSpace(request.Id)) {
            var id = await CreateDeviceIdAsync();

            var entity = new OpcDevice {
                ID = id,
                DEVICE_NAME = request.DeviceName,
                DEVICE_ADDRESS = request.DeviceAddress,
                PORT = request.Port,
                ENDPOINT_URL = endpointUrl,
                DEVICE_TYPE = request.DeviceType,
                IS_COLLECTENABLED = request.IsCollectEnabled,
                USESECURITY = request.UseSecurity,
                SECURITYPOLICY = request.SecurityPolicy,
                SECURITYMODE = request.SecurityMode,
                USE_ANONYMOUS = request.UseAnonymous,
                USER_NAME = request.UserName,
                PASSWORD = request.Password,
                PUBLISHINGINTERVALMS = request.PublishingIntervalMs,
                SAMPLINGINTERVALMS = request.SamplingIntervalMs,
                DESCRIPTION = request.Description,
                IsEnabled = request.IsEnabled,
                CreatedAt = now,
                UpdatedAt = now
            };

            _db.Set<OpcDevice>().Add(entity);
            await _db.SaveChangesAsync();

            return Json(new {
                success = true,
                message = "디바이스가 신규 저장되었습니다.",
                data = new { id }
            });
        }

        var device = await _db.Set<OpcDevice>()
            .FirstOrDefaultAsync(x => x.ID == request.Id);

        if (device == null) {
            return Json(new {
                success = false,
                message = "수정할 디바이스를 찾을 수 없습니다."
            });
        }

        device.DEVICE_NAME = request.DeviceName;
        device.DEVICE_ADDRESS = request.DeviceAddress;
        device.PORT = request.Port;
        device.ENDPOINT_URL = endpointUrl;
        device.DEVICE_TYPE = request.DeviceType;
        device.IS_COLLECTENABLED = request.IsCollectEnabled;
        device.USESECURITY = request.UseSecurity;
        device.SECURITYPOLICY = request.SecurityPolicy;
        device.SECURITYMODE = request.SecurityMode;
        device.USE_ANONYMOUS = request.UseAnonymous;
        device.USER_NAME = request.UserName;
        device.PASSWORD = request.Password;
        device.PUBLISHINGINTERVALMS = request.PublishingIntervalMs;
        device.SAMPLINGINTERVALMS = request.SamplingIntervalMs;
        device.DESCRIPTION = request.Description;
        device.IsEnabled = request.IsEnabled;
        device.UpdatedAt = now;

        await _db.SaveChangesAsync();

        return Json(new {
            success = true,
            message = "디바이스가 수정되었습니다.",
            data = new { id = device.ID }
        });
    }

    [HttpPost, ActionName("delete")]
    public async Task<IActionResult> Delete([FromBody] TestDeviceDeleteRequest request) {
        if (string.IsNullOrWhiteSpace(request.Id)) {
            return Json(new {
                success = false,
                message = "삭제할 디바이스를 선택해 주세요."
            });
        }

        var device = await _db.Set<OpcDevice>()
            .FirstOrDefaultAsync(x => x.ID == request.Id);

        if (device == null) {
            return Json(new {
                success = false,
                message = "삭제할 디바이스를 찾을 수 없습니다."
            });
        }

        var hasTags = await _db.Set<OpcTag>()
            .AnyAsync(x => x.DEVICE_ID == request.Id);

        if (hasTags) {
            return Json(new {
                success = false,
                message = "등록된 태그가 있는 디바이스는 삭제할 수 없습니다. 태그를 먼저 삭제해 주세요."
            });
        }

        _db.Set<OpcDevice>().Remove(device);
        await _db.SaveChangesAsync();

        return Json(new {
            success = true,
            message = "디바이스가 삭제되었습니다."
        });
    }

    [HttpGet, ActionName("endpoint-preview")]
    public IActionResult EndpointPreview(string? deviceType, string? address, int port = 0) {
        var endpointUrl = "";

        if (deviceType == "OPCUA" && !string.IsNullOrWhiteSpace(address) && port > 0) {
            endpointUrl = $"opc.tcp://{address}:{port}";
        }

        return Json(new {
            success = true,
            message = "생성되었습니다.",
            data = new {
                endpointUrl
            }
        });
    }

    private async Task<string> CreateDeviceIdAsync() {
        var ids = await _db.Set<OpcDevice>()
            .AsNoTracking()
            .Select(x => x.ID)
            .ToListAsync();

        var max = ids
            .Where(x => x.StartsWith("DVC"))
            .Select(x => x.Replace("DVC", ""))
            .Where(x => int.TryParse(x, out _))
            .Select(int.Parse)
            .DefaultIfEmpty(0)
            .Max();

        return $"DVC{max + 1:0000}";
    }

    private static string CreateEndpointUrl(DeviceSaveRequest request) {
        if (!string.IsNullOrWhiteSpace(request.EndpointUrl)) {
            return request.EndpointUrl;
        }

        if (request.DeviceType == "OPCUA") {
            return $"opc.tcp://{request.DeviceAddress}:{request.Port}";
        }

        return "";
    }
}

public class TestDeviceDeleteRequest {
    public string? Id { get; set; }
}