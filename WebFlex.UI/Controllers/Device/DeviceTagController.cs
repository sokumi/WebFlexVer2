using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebFlex.Shared;
using WebFlex.UI.Data;
using WebFlex.UI.DTO.Common;
using WebFlex.UI.DTO.Device;
using WebFlex.UI.Services.Device;

namespace WebFlex.UI.Controllers.Device;

[Route("device/tag/[action]")]
public class DeviceTagController : Controller {
    private readonly WebFlexDbContext _db;
    private readonly OpcBrowseService _opcBrowseService;

    public DeviceTagController(
        WebFlexDbContext db,
        OpcBrowseService opcBrowseService) {
        _db = db;
        _opcBrowseService = opcBrowseService;
    }

    [HttpGet, ActionName("list")]
    public async Task<IActionResult> TagList(string deviceId) {
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
     string deviceId,
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

    [HttpPost, ActionName("insert")]
    public async Task<IActionResult> TagInsert([FromBody] DeviceTagSaveRequest request) {
        if (request == null || string.IsNullOrWhiteSpace(request.DeviceId)) {
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

        var tagPrefix = $"GT{DateTime.Now:yyMM}";

        var lastTagCode = await _db.OpcTags
            .Where(x => x.TagCode.StartsWith(tagPrefix))
            .OrderByDescending(x => x.TagCode)
            .Select(x => x.TagCode)
            .FirstOrDefaultAsync();

        var nextTagNo = 1;

        if (!string.IsNullOrWhiteSpace(lastTagCode) &&
            lastTagCode.Length >= tagPrefix.Length + 4 &&
            int.TryParse(lastTagCode.Substring(tagPrefix.Length), out var lastTagNo)) {
            nextTagNo = lastTagNo + 1;
        }

        foreach (var node in request.Nodes) {
            if (string.IsNullOrWhiteSpace(node.NodeId)) {
                continue;
            }

            if (existingSet.Contains(node.NodeId)) {
                continue;
            }

            sortOrder++;

            var tagCode = $"{tagPrefix}{nextTagNo++:D4}";

            var tag = new OpcTag {
                Id = tagCode,
                OpcDeviceId = device.Id,
                OpcGroupId = group.Id,
                TagCode = tagCode,
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

        var groupCode = await CreateGroupCodeAsync();

        group = new OpcGroup {
            Id = groupCode,
            GroupCode = groupCode,
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


    public class DeleteTagRequest {
        public string[] Ids { get; set; } = [];
    }

    [HttpPost, ActionName("delete")]
    public async Task<IActionResult> TagDelete([FromBody] DeleteTagRequest request) {
        if (request.Ids == null || request.Ids.Length == 0) {
            return Json(ApiResponse<bool>.Fail("삭제할 태그를 선택하세요."));
        }

        var tags = await _db.OpcTags
            .Where(x => request.Ids.Contains(x.Id))
            .ToListAsync();

        if (tags.Count == 0) {
            return Json(ApiResponse<bool>.Fail("삭제할 태그를 찾을 수 없습니다."));
        }

        _db.OpcTags.RemoveRange(tags);
        await _db.SaveChangesAsync();

        return Json(ApiResponse<bool>.Ok(true, $"{tags.Count}개의 태그가 삭제되었습니다."));
    }

}