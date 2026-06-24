using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebFlex.Shared;
using WebFlex.UI.Data;
using WebFlex.UI.DTO.Common;
using WebFlex.UI.DTO.Device;
using WebFlex.UI.Services.Device;
using Npgsql;
using NpgsqlTypes;

namespace WebFlex.UI.Controllers.Device;

[Route("device/tag/[action]")]
public class DeviceTagController : Controller {
    private readonly WebFlexDbContext _db;
    private readonly OpcBrowseService _opcBrowseService;
    private readonly IConfiguration _configuration;

    public DeviceTagController(
        WebFlexDbContext db,
        OpcBrowseService opcBrowseService,
        IConfiguration configuration) {
        _db = db;
        _opcBrowseService = opcBrowseService;
        _configuration = configuration;
    }

    [HttpGet, ActionName("list")]
    public async Task<IActionResult> TagList(string deviceId) {
        var data = await _db.Set<OpcTag>()
            .AsNoTracking()
            .Where(x => x.DEVICE_ID == deviceId)
            .OrderBy(x => x.SORT_ORDER)
            .ThenBy(x => x.TAG_NAME)
            .Select(x => new DeviceTagDto {
                Id = x.ID,
                TagName = x.TAG_NAME,
                NodeId = x.NODE_ID,
                GroupId = x.GROUP_ID,
                DataType = x.DATA_TYPE,
                IsCollectEnabled = x.IS_COLLECTENABLED,
                SaveToDatabase = x.SAVE_TO_DATABASE,
                ShowOnDashboard = x.SHOW_ON_DASHBOARD,
                SamplingIntervalMs = x.SAMPLINGINTERVALMS,
                SortOrder = x.SORT_ORDER,
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
        var device = await _db.Set<OpcDevice>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ID == deviceId, cancellationToken);

        if (device == null) {
            return Json(ApiResponse<List<DeviceNodeDto>>.Fail("디바이스 정보를 찾을 수 없습니다."));
        }

        if (!device.DEVICE_TYPE.Equals("OPCUA", StringComparison.OrdinalIgnoreCase)) {
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

        var device = await _db.Set<OpcDevice>()
            .FirstOrDefaultAsync(x => x.ID == request.DeviceId);

        if (device == null) {
            return Json(ApiResponse<bool>.Fail("디바이스 정보를 찾을 수 없습니다."));
        }

        var now = DateTime.UtcNow;

        var group = await GetOrCreateDeviceGroupAsync(device, now);

        var existingNodeIds = await _db.Set<OpcTag>()
            .Where(x => x.DEVICE_ID == device.ID)
            .Select(x => x.NODE_ID)
            .ToListAsync();

        var existingSet = existingNodeIds.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var sortOrder = await _db.Set<OpcTag>()
            .Where(x => x.DEVICE_ID == device.ID)
            .MaxAsync(x => (int?)x.SORT_ORDER) ?? 0;

        var tagPrefix = $"GT{DateTime.Now:yyMM}";

        var lastTagCode = await _db.Set<OpcTag>()
            .Where(x => x.ID.StartsWith(tagPrefix))
            .OrderByDescending(x => x.ID)
            .Select(x => x.ID)
            .FirstOrDefaultAsync();

        var nextTagNo = 1;

        if (!string.IsNullOrWhiteSpace(lastTagCode) &&
            lastTagCode.Length >= tagPrefix.Length + 4 &&
            int.TryParse(lastTagCode.Substring(tagPrefix.Length), out var lastTagNo)) {
            nextTagNo = lastTagNo + 1;
        }

        var newTags = new List<OpcTag>();

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
                ID = tagCode,
                DEVICE_ID = device.ID,
                GROUP_ID = group.ID,
                NODE_ID = node.NodeId,
                TAG_NAME = string.IsNullOrWhiteSpace(node.DisplayName)
                    ? node.NodeId
                    : node.DisplayName,
                //GroupName = group.GroupName,
                DATA_TYPE = node.DataType,
                IS_COLLECTENABLED = true,
                SAVE_TO_DATABASE = true,
                SHOW_ON_DASHBOARD = false,
                SAMPLINGINTERVALMS = device.SAMPLINGINTERVALMS,
                SORT_ORDER = sortOrder,
                DESCRIPTION = node.NodeId,
                IsEnabled = true,
                CreatedAt = now,
                UpdatedAt = now
            };

            _db.Set<OpcTag>().Add(tag);
            newTags.Add(tag);
        }

        await _db.SaveChangesAsync();

        await InsertCurrentValuesAsync(newTags);


        return Json(ApiResponse<bool>.Ok(true, "태그가 저장되었습니다."));
    }

    private async Task<OpcGroup> GetOrCreateDeviceGroupAsync(OpcDevice device, DateTime now) {
        var group = await _db.Set<OpcGroup>()
            .FirstOrDefaultAsync(x => x.GROUP_NAME == device.DEVICE_NAME);

        if (group != null) {
            return group;
        }

        var groupCode = await CreateGroupCodeAsync();

        group = new OpcGroup {
            ID = groupCode,
            GROUP_NAME = device.DEVICE_NAME,
            SORT_ORDER = 0,
            DESCRIPTION = $"{device.DEVICE_NAME} 자동 생성 그룹",
            IsEnabled = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.Set<OpcGroup>().Add(group);

        await _db.SaveChangesAsync();

        return group;
    }

    private async Task<string> CreateGroupCodeAsync() {
        var prefix = $"DG{DateTime.Now:yyMM}";
        var lastCode = await _db.Set<OpcGroup>()
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



    public class DeleteTagRequest {
        public string[] Ids { get; set; } = [];
    }

    [HttpPost, ActionName("delete")]
    public async Task<IActionResult> TagDelete([FromBody] DeleteTagRequest request) {
        if (request.Ids == null || request.Ids.Length == 0) {
            return Json(ApiResponse<bool>.Fail("삭제할 태그를 선택하세요."));
        }

        var tags = await _db.Set<OpcTag>()
            .Where(x => request.Ids.Contains(x.ID))
            .ToListAsync();

        if (tags.Count == 0) {
            return Json(ApiResponse<bool>.Fail("삭제할 태그를 찾을 수 없습니다."));
        }

        _db.Set<OpcTag>().RemoveRange(tags);
        await _db.SaveChangesAsync();

        await DeleteCurrentValuesAsync(tags.Select(x => x.ID).ToArray());

        return Json(ApiResponse<bool>.Ok(true, $"{tags.Count}개의 태그가 삭제되었습니다."));
    }

    private async Task InsertCurrentValuesAsync(IReadOnlyList<OpcTag> tags) {
        if (tags.Count == 0)
            return;

        var connectionString = _configuration.GetConnectionString("WebFlexTsd")
            ?? throw new InvalidOperationException("ConnectionStrings:WebFlexTsd 설정이 없습니다.");

        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(@"
INSERT INTO public.currentvalue
(
    tag_id,
    group_id,
    value,
    status,
    cookie_value,
    update_count,
    source_timestamp,
    received_at,
    updated_at
)
SELECT
    unnest(@tag_ids),
    unnest(@group_ids),
    NULL,
    NULL,
    NULL,
    0,
    NULL,
    now(),
    now()
ON CONFLICT (tag_id)
DO UPDATE SET
    group_id = EXCLUDED.group_id,
    updated_at = now();
", conn);

        cmd.Parameters.Add(new NpgsqlParameter("@tag_ids", NpgsqlDbType.Array | NpgsqlDbType.Text) {
            Value = tags.Select(x => x.ID).ToArray()
        });

        cmd.Parameters.Add(new NpgsqlParameter("@group_ids", NpgsqlDbType.Array | NpgsqlDbType.Text) {
            Value = tags.Select(x => (object?)x.GROUP_ID ?? DBNull.Value).ToArray()
        });

        await cmd.ExecuteNonQueryAsync();
    }

    private async Task DeleteCurrentValuesAsync(IReadOnlyList<string> tagIds) {
        if (tagIds.Count == 0)
            return;

        var connectionString = _configuration.GetConnectionString("WebFlexTsd")
            ?? throw new InvalidOperationException("ConnectionStrings:WebFlexTsd 설정이 없습니다.");

        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(@"
DELETE FROM public.currentvalue
WHERE tag_id = ANY(@tag_ids);
", conn);

        cmd.Parameters.Add(new NpgsqlParameter("@tag_ids", NpgsqlDbType.Array | NpgsqlDbType.Text) {
            Value = tagIds.ToArray()
        });

        await cmd.ExecuteNonQueryAsync();
    }

}