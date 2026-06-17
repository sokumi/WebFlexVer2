using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebFlex.Shared.Entities.Opc;
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

    [HttpGet, ActionName("tag-list")]
    public async Task<IActionResult> TagList(long deviceId) {
        var data = await _db.OpcTags
            .AsNoTracking()
            .Where(x => x.OpcDeviceId == deviceId)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.DisplayName)
            .Select(x => new DeviceTagDto {
                Id = x.Id,
                TagCode = x.TagCode,
                NodeId = x.NodeId,
                DisplayName = x.DisplayName,
                GroupName = x.GroupName,
                DataType = x.DataType,
                IsCollectEnabled = x.IsCollectEnabled,
                SaveToDatabase = x.SaveToDatabase,
                ShowOnDashboard = x.ShowOnDashboard,
                SamplingIntervalMs = x.SamplingIntervalMs,
                QueueSize = x.QueueSize,
                SortOrder = x.SortOrder,
                IsEnabled = x.IsEnabled
            })
            .ToListAsync();

        return Json(ApiResponse<List<DeviceTagDto>>.Ok(data));
    }

    [HttpGet, ActionName("browse")]
    public async Task<IActionResult> Browse(
     long deviceId,
     bool onlyCollectable = true,
     CancellationToken cancellationToken = default) {
        var device = await _db.OpcDevices
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == deviceId, cancellationToken);

        if (device == null) {
            return Json(ApiResponse<List<DeviceNodeDto>>.Fail("디바이스 정보를 찾을 수 없습니다."));
        }

        if (!device.DeviceType.Equals("OPCUA", StringComparison.OrdinalIgnoreCase)) {
            return Json(ApiResponse<List<DeviceNodeDto>>.Fail("OPC UA 디바이스만 노드 조회가 가능합니다."));
        }

        try {
            var nodes = await _opcBrowseService.BrowseAsync(
     device,
     onlyCollectable,
     cancellationToken);
            return Json(ApiResponse<List<DeviceNodeDto>>.Ok(nodes));
        } catch (Exception ex) {
            return Json(ApiResponse<List<DeviceNodeDto>>.Fail(ex.Message));
        }
    }

    [HttpPost, ActionName("tag-save")]
    public async Task<IActionResult> TagSave([FromBody] DeviceTagSaveRequest request) {
        if (request.DeviceId <= 0) {
            return Json(ApiResponse<bool>.Fail("디바이스를 선택하세요."));
        }

        if (request.Nodes.Count == 0) {
            return Json(ApiResponse<bool>.Fail("저장할 노드를 선택하세요."));
        }

        var device = await _db.OpcDevices
            .FirstOrDefaultAsync(x => x.Id == request.DeviceId);

        if (device == null) {
            return Json(ApiResponse<bool>.Fail("디바이스 정보를 찾을 수 없습니다."));
        }

        var now = DateTime.UtcNow;

        var group = await GetOrCreateDeviceGroupAsync(device, now);

        var existingNodeIds = await _db.OpcTags
            .Where(x => x.OpcDeviceId == device.Id)
            .Select(x => x.NodeId)
            .ToListAsync();

        var existingSet = existingNodeIds.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var sortOrder = await _db.OpcTags
            .Where(x => x.OpcDeviceId == device.Id)
            .MaxAsync(x => (int?)x.SortOrder) ?? 0;

        foreach (var node in request.Nodes) {
            if (string.IsNullOrWhiteSpace(node.NodeId)) {
                continue;
            }

            if (existingSet.Contains(node.NodeId)) {
                continue;
            }

            sortOrder++;

            var tag = new OpcTag {
                OpcDeviceId = device.Id,
                OpcGroupId = group.Id,
                TagCode = await CreateTagCodeAsync(),
                NodeId = node.NodeId,
                DisplayName = string.IsNullOrWhiteSpace(node.DisplayName)
                    ? node.NodeId
                    : node.DisplayName,
                GroupName = group.GroupName,
                DataType = node.DataType,
                IsCollectEnabled = true,
                SaveToDatabase = true,
                ShowOnDashboard = false,
                SamplingIntervalMs = device.SamplingIntervalMs,
                QueueSize = device.QueueSize,
                SortOrder = sortOrder,
                Description = node.NodeId,
                IsEnabled = true,
                CreatedAt = now,
                UpdatedAt = now
            };

            _db.OpcTags.Add(tag);
        }

        await _db.SaveChangesAsync();

        return Json(ApiResponse<bool>.Ok(true, "태그가 저장되었습니다."));
    }

    private async Task<OpcGroup> GetOrCreateDeviceGroupAsync(OpcDevice device, DateTime now) {
        var group = await _db.OpcGroups
            .FirstOrDefaultAsync(x => x.GroupName == device.DeviceName);

        if (group != null) {
            return group;
        }

        group = new OpcGroup {
            GroupCode = await CreateGroupCodeAsync(),
            GroupName = device.DeviceName,
            SortOrder = 0,
            Description = $"{device.DeviceName} 자동 생성 그룹",
            IsEnabled = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.OpcGroups.Add(group);

        await _db.SaveChangesAsync();

        return group;
    }

    private async Task<string> CreateGroupCodeAsync() {
        var prefix = $"DG{DateTime.Now:yyMM}";
        var lastCode = await _db.OpcGroups
            .Where(x => x.GroupCode.StartsWith(prefix))
            .OrderByDescending(x => x.GroupCode)
            .Select(x => x.GroupCode)
            .FirstOrDefaultAsync();

        var nextNo = 1;

        if (!string.IsNullOrWhiteSpace(lastCode) &&
            lastCode.Length >= prefix.Length + 4 &&
            int.TryParse(lastCode.Substring(prefix.Length), out var lastNo)) {
            nextNo = lastNo + 1;
        }

        return $"{prefix}{nextNo:D4}";
    }

    private async Task<string> CreateTagCodeAsync() {
        var prefix = $"GT{DateTime.Now:yyMM}";
        var lastCode = await _db.OpcTags
            .Where(x => x.TagCode.StartsWith(prefix))
            .OrderByDescending(x => x.TagCode)
            .Select(x => x.TagCode)
            .FirstOrDefaultAsync();

        var nextNo = 1;

        if (!string.IsNullOrWhiteSpace(lastCode) &&
            lastCode.Length >= prefix.Length + 4 &&
            int.TryParse(lastCode.Substring(prefix.Length), out var lastNo)) {
            nextNo = lastNo + 1;
        }

        return $"{prefix}{nextNo:D4}";
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

        if (request.Id.HasValue && request.Id.Value > 0) {
            entity = await _db.OpcDevices
                .FirstOrDefaultAsync(x => x.Id == request.Id.Value);

            if (entity == null) {
                return Json(ApiResponse<bool>.Fail("디바이스 정보를 찾을 수 없습니다."));
            }
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

    [HttpPost, ActionName("delete")]
    public async Task<IActionResult> Delete([FromBody] long id) {
        var entity = await _db.OpcDevices.FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null) {
            return Json(ApiResponse<bool>.Fail("디바이스 정보를 찾을 수 없습니다."));
        }

        _db.OpcDevices.Remove(entity);

        await _db.SaveChangesAsync();

        return Json(ApiResponse<bool>.Ok(true, "삭제되었습니다."));
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
}