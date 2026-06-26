using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebFlex.Shared;
using WebFlex.UI.Data;
using WebFlex.UI.DTO;
using WebFlex.UI.Services;
using Npgsql;
using NpgsqlTypes;

namespace WebFlex.UI.Controllers.Device;

[Route("device/tag/[action]")]
public class DeviceTagController : Controller {
    private readonly WebFlexDbContext _db;
    private readonly OpcBrowseService _opcBrowseService;

    public DeviceTagController(
        WebFlexDbContext db,
        OpcBrowseService opcBrowseService,
        IConfiguration configuration) {
        _db = db;
        _opcBrowseService = opcBrowseService;
    }

    [HttpGet, ActionName("devices")]
    public async Task<IActionResult> DevicesList() {
        var rows = await _db.Set<OpcDevice>()
            .AsNoTracking()
            .OrderBy(x => x.DEVICE_NAME)
            .Select(x => new {
                id = x.ID,
                deviceName = x.DEVICE_NAME,
                deviceType = x.DEVICE_TYPE,
                endpointUrl = x.ENDPOINT_URL,
                isCollectEnabled = x.IS_COLLECTENABLED,
                tagCount = _db.Set<OpcTag>().Count(t => t.DEVICE_ID == x.ID)
            })
            .ToListAsync();

        return Json(new {
            success = true,
            message = "조회되었습니다.",
            data = rows
        });
    }

    [HttpGet, ActionName("summary")]
    public async Task<IActionResult> Summary(string? deviceId = null) {
        var deviceQuery = _db.Set<OpcDevice>().AsNoTracking();
        var tagQuery = _db.Set<OpcTag>().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(deviceId)) {
            tagQuery = tagQuery.Where(x => x.DEVICE_ID == deviceId);
        }

        var deviceCount = await deviceQuery.CountAsync();
        var tagCount = await tagQuery.CountAsync();
        var collectTagCount = await tagQuery.CountAsync(x => x.IS_COLLECTENABLED);

        return Json(new {
            success = true,
            message = "조회되었습니다.",
            data = new {
                deviceCount,
                nodeCount = 0,
                variableNodeCount = 0,
                tagCount,
                collectTagCount
            }
        });
    }

    [HttpGet, ActionName("check-connection")]
    public async Task<IActionResult> CheckConnection(
    string deviceId,
    CancellationToken cancellationToken = default) {
        if (string.IsNullOrWhiteSpace(deviceId)) {
            return Json(new {
                success = false,
                message = "디바이스를 선택해 주세요."
            });
        }

        var device = await _db.Set<OpcDevice>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ID == deviceId, cancellationToken);

        if (device == null) {
            return Json(new {
                success = false,
                message = "디바이스를 찾을 수 없습니다."
            });
        }

        if (string.IsNullOrWhiteSpace(device.ENDPOINT_URL)) {
            return Json(new {
                success = true,
                message = "Endpoint URL이 없습니다.",
                data = new {
                    connected = false,
                    errorMessage = "Endpoint URL이 없습니다."
                }
            });
        }

        try {
            await _opcBrowseService.CheckConnectionAsync(device, cancellationToken);

            return Json(new {
                success = true,
                message = "연결되었습니다.",
                data = new {
                    connected = true,
                    errorMessage = ""
                }
            });
        } catch (Exception ex) {
            return Json(new {
                success = true,
                message = "연결 실패",
                data = new {
                    connected = false,
                    errorMessage = ex.Message
                }
            });
        }
    }

    [HttpGet, ActionName("browse")]
    public async Task<IActionResult> Browse(
        string deviceId,
        bool onlyCollectable = true,
        CancellationToken cancellationToken = default) {
        if (string.IsNullOrWhiteSpace(deviceId)) {
            return Json(new {
                success = false,
                message = "디바이스를 선택해 주세요."
            });
        }

        var device = await _db.Set<OpcDevice>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ID == deviceId, cancellationToken);

        if (device == null) {
            return Json(new {
                success = false,
                message = "디바이스를 찾을 수 없습니다."
            });
        }

        if (string.IsNullOrWhiteSpace(device.ENDPOINT_URL)) {
            return Json(new {
                success = false,
                message = "Endpoint URL이 없습니다."
            });
        }

        try {
            var nodes = await _opcBrowseService.BrowseAsync(
                device,
                onlyCollectable,
                cancellationToken
            );

            return Json(new {
                success = true,
                message = "OPC 노드를 조회했습니다.",
                data = nodes
            });
        } catch (Exception ex) {
            return Json(new {
                success = false,
                message = $"OPC 노드 조회 중 오류가 발생했습니다. {ex.Message}"
            });
        }
    }

    [HttpGet, ActionName("list")]
    public async Task<IActionResult> List(string deviceId, string? keyword = null, bool onlyCollect = false) {
        if (string.IsNullOrWhiteSpace(deviceId)) {
            return Json(new {
                success = false,
                message = "디바이스를 선택해 주세요."
            });
        }

        var query = _db.Set<OpcTag>()
            .AsNoTracking()
            .Where(x => x.DEVICE_ID == deviceId);

        if (!string.IsNullOrWhiteSpace(keyword)) {
            query = query.Where(x =>
                x.ID.Contains(keyword) ||
                x.NODE_ID.Contains(keyword) ||
                (x.TAG_NAME != null && x.TAG_NAME.Contains(keyword))
            );
        }

        if (onlyCollect) {
            query = query.Where(x => x.IS_COLLECTENABLED);
        }

        var rows = await query
            .OrderBy(x => x.SORT_ORDER)
            .ThenBy(x => x.TAG_NAME)
            .Select(x => new {
                id = x.ID,
                deviceId = x.DEVICE_ID,
                groupId = x.GROUP_ID,
                tagName = x.TAG_NAME,
                displayName = x.TAG_NAME,
                nodeId = x.NODE_ID,
                dataType = x.DATA_TYPE,
                isCollectEnabled = x.IS_COLLECTENABLED,
                saveToDatabase = x.SAVE_TO_DATABASE,
                samplingIntervalMs = x.SAMPLINGINTERVALMS,
                queueSize = 1,
                sortOrder = x.SORT_ORDER,
                description = x.DESCRIPTION
            })
            .ToListAsync();

        return Json(new {
            success = true,
            message = "조회되었습니다.",
            data = rows
        });
    }

    [HttpPost, ActionName("save")]
    public async Task<IActionResult> Save([FromBody] TestDeviceTagSaveRequest request) {
        if (string.IsNullOrWhiteSpace(request.DeviceId)) {
            return Json(new {
                success = false,
                message = "디바이스를 선택해 주세요."
            });
        }

        if (request.Nodes == null || request.Nodes.Count == 0) {
            return Json(new {
                success = false,
                message = "저장할 노드를 선택해 주세요."
            });
        }

        var device = await _db.Set<OpcDevice>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ID == request.DeviceId);

        if (device == null) {
            return Json(new {
                success = false,
                message = "디바이스를 찾을 수 없습니다."
            });
        }

        var variableNodes = request.Nodes
            .Where(x => x.NodeClass == "Variable")
            .ToList();

        if (variableNodes.Count == 0) {
            return Json(new {
                success = false,
                message = "Variable 노드만 태그로 저장할 수 있습니다."
            });
        }

        var now = DateTime.UtcNow;
        var insertCount = 0;
        var skipCount = 0;

        foreach (var node in variableNodes) {
            var exists = await _db.Set<OpcTag>()
                .AnyAsync(x => x.DEVICE_ID == request.DeviceId && x.NODE_ID == node.NodeId);

            if (exists) {
                skipCount++;
                continue;
            }

            var tagId = await CreateTagIdAsync();

            var tag = new OpcTag {
                ID = tagId,
                DEVICE_ID = request.DeviceId,
                GROUP_ID = null,
                NODE_ID = node.NodeId,
                TAG_NAME = string.IsNullOrWhiteSpace(node.DisplayName)
                    ? node.NodeId
                    : node.DisplayName,
                DATA_TYPE = node.DataType,
                IS_COLLECTENABLED = true,
                SAVE_TO_DATABASE = true,
                SHOW_ON_DASHBOARD = false,
                SAMPLINGINTERVALMS = device.SAMPLINGINTERVALMS ?? 1000,
                SORT_ORDER = await CreateSortOrderAsync(request.DeviceId),
                DESCRIPTION = node.Description,
                IsEnabled = true,
                CreatedAt = now,
                UpdatedAt = now
            };

            _db.Set<OpcTag>().Add(tag);
            insertCount++;
        }

        await _db.SaveChangesAsync();

        return Json(new {
            success = true,
            message = $"저장 완료: 신규 {insertCount}개, 중복 제외 {skipCount}개",
            data = new {
                insertCount,
                skipCount
            }
        });
    }

    [HttpPost, ActionName("delete")]
    public async Task<IActionResult> Delete([FromBody] TestDeviceTagDeleteRequest request) {
        if (request.Ids == null || request.Ids.Count == 0) {
            return Json(new {
                success = false,
                message = "삭제할 태그를 선택해 주세요."
            });
        }

        var tags = await _db.Set<OpcTag>()
            .Where(x => request.Ids.Contains(x.ID))
            .ToListAsync();

        if (tags.Count == 0) {
            return Json(new {
                success = false,
                message = "삭제할 태그를 찾을 수 없습니다."
            });
        }

        _db.Set<OpcTag>().RemoveRange(tags);
        await _db.SaveChangesAsync();

        return Json(new {
            success = true,
            message = $"{tags.Count}개의 태그가 삭제되었습니다."
        });
    }

    private async Task<string> CreateTagIdAsync() {
        var ids = await _db.Set<OpcTag>()
            .AsNoTracking()
            .Select(x => x.ID)
            .ToListAsync();

        var max = ids
            .Where(x => x.StartsWith("TAG"))
            .Select(x => x.Replace("TAG", ""))
            .Where(x => int.TryParse(x, out _))
            .Select(int.Parse)
            .DefaultIfEmpty(0)
            .Max();

        return $"TAG{max + 1:0000}";
    }

    private async Task<int> CreateSortOrderAsync(string deviceId) {
        var max = await _db.Set<OpcTag>()
            .Where(x => x.DEVICE_ID == deviceId)
            .Select(x => x.SORT_ORDER ?? 0)
            .DefaultIfEmpty(0)
            .MaxAsync();

        return max + 1;
    }
}

public class TestDeviceTagSaveRequest {
    public string DeviceId { get; set; } = "";
    public List<TestDeviceTagSaveNode> Nodes { get; set; } = new();
}

public class TestDeviceTagSaveNode {
    public string NodeId { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string NodeClass { get; set; } = "";
    public string DataType { get; set; } = "";
    public string? Description { get; set; }
}

public class TestDeviceTagDeleteRequest {
    public List<string> Ids { get; set; } = new();
}