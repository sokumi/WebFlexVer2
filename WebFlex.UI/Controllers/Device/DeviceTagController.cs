using DynamicExpresso;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using System.Globalization;
using System.Text.Json;
using WebFlex.Shared;
using WebFlex.Shared.Exceptions;
using WebFlex.UI.Common;
using WebFlex.UI.Data;
using WebFlex.UI.Services;

namespace WebFlex.UI.Controllers.Device;

[Route("device/tag/[action]")]
public class DeviceTagController : WebFlexController {
    private readonly WebFlexDbContext _db;
    private readonly TsdReadDbContext _tsdDb;
    private readonly OpcBrowseService _opcBrowseService;
    private readonly INewNoService _newNo;

    public DeviceTagController(
        WebFlexDbContext db,
        TsdReadDbContext tsdDb,
        OpcBrowseService opcBrowseService,
        INewNoService newNo) {
        _db = db;
        _tsdDb = tsdDb;
        _opcBrowseService = opcBrowseService;
        _newNo = newNo;
    }

    [HttpGet, ActionName("devices")]
    public async Task<IActionResult> DevicesList() {
        try {
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

            return Success("조회되었습니다.", rows);
        } catch (Exception ex) {
            return ErrorData(GetErrorMessage(ex));
        }
    }

    [HttpGet, ActionName("summary")]
    public async Task<IActionResult> Summary(string? deviceId = null) {
        try {
            var deviceQuery = _db.Set<OpcDevice>().AsNoTracking();
            var tagQuery = _db.Set<OpcTag>().AsNoTracking();

            if (!string.IsNullOrWhiteSpace(deviceId)) {
                tagQuery = tagQuery.Where(x => x.DEVICE_ID == deviceId);
            }

            var deviceCount = await deviceQuery.CountAsync();
            var tagCount = await tagQuery.CountAsync();
            var collectTagCount = await tagQuery.CountAsync(x => x.IS_COLLECTENABLED);

            return Success("조회되었습니다.", new {
                deviceCount,
                nodeCount = 0,
                variableNodeCount = 0,
                tagCount,
                collectTagCount
            });
        } catch (Exception ex) {
            return ErrorData(GetErrorMessage(ex));
        }
    }

    [HttpGet, ActionName("check-connection")]
    public async Task<IActionResult> CheckConnection(
        string deviceId,
        CancellationToken cancellationToken = default) {
        try {
            if (string.IsNullOrWhiteSpace(deviceId)) {
                return ErrorData("디바이스를 선택해 주세요.");
            }

            var device = await _db.Set<OpcDevice>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ID == deviceId, cancellationToken);

            if (device == null) {
                return ErrorData("디바이스를 찾을 수 없습니다.");
            }

            if (string.IsNullOrWhiteSpace(device.ENDPOINT_URL)) {
                return Success("Endpoint URL이 없습니다.", new {
                    connected = false,
                    errorMessage = "Endpoint URL이 없습니다."
                });
            }

            try {
                await _opcBrowseService.CheckConnectionAsync(device, cancellationToken);

                return Success("연결되었습니다.", new {
                    connected = true,
                    errorMessage = ""
                });
            } catch (Exception ex) {
                return Success("연결 실패", new {
                    connected = false,
                    errorMessage = ex.Message
                });
            }
        } catch (Exception ex) {
            return ErrorData(GetErrorMessage(ex));
        }
    }

    [HttpGet, ActionName("browse")]
    public async Task<IActionResult> Browse(
        string deviceId,
        bool onlyCollectable = true,
        CancellationToken cancellationToken = default) {
        try {
            if (string.IsNullOrWhiteSpace(deviceId)) {
                return ErrorData("디바이스를 선택해 주세요.");
            }

            var device = await _db.Set<OpcDevice>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ID == deviceId, cancellationToken);

            if (device == null) {
                return ErrorData("디바이스를 찾을 수 없습니다.");
            }

            if (string.IsNullOrWhiteSpace(device.ENDPOINT_URL)) {
                return ErrorData("Endpoint URL이 없습니다.");
            }

            var nodes = await _opcBrowseService.BrowseAsync(
                device,
                onlyCollectable,
                cancellationToken
            );

            return Success("OPC 노드를 조회했습니다.", nodes);
        } catch (Exception ex) {
            return ErrorData($"OPC 노드 조회 중 오류가 발생했습니다. {GetErrorMessage(ex)}");
        }
    }

    [HttpGet, ActionName("list")]
    public async Task<IActionResult> List(string deviceId, string? keyword = null, bool onlyCollect = false) {
        try {
            if (string.IsNullOrWhiteSpace(deviceId)) {
                return ErrorData("디바이스를 선택해 주세요.");
            }

            var query = _db.Set<OpcTag>()
                .AsNoTracking()
                .Where(x => x.DEVICE_ID == deviceId);

            if (!string.IsNullOrWhiteSpace(keyword)) {
                query = query.Where(x =>
                    x.ID.Contains(keyword) ||
                    x.NODE_ID.Contains(keyword) ||
                    (x.TAG_NAME != null && x.TAG_NAME.Contains(keyword)) ||
                    (x.DESCRIPTION != null && x.DESCRIPTION.Contains(keyword)) ||
                    (x.EXPRESSIONS != null && x.EXPRESSIONS.Contains(keyword))
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
                    nodeId = x.NODE_ID,
                    groupId = x.GROUP_ID,
                    tagName = x.TAG_NAME,
                    dataType = x.DATA_TYPE,
                    protectType = x.PROTECT_TYPE,
                    isCollectEnabled = x.IS_COLLECTENABLED,
                    saveToDatabase = x.SAVE_TO_DATABASE,
                    showOnDashboard = x.SHOW_ON_DASHBOARD,
                    samplingIntervalMs = x.SAMPLINGINTERVALMS,
                    sortOrder = x.SORT_ORDER,
                    description = x.DESCRIPTION,
                    expressions = x.EXPRESSIONS,
                    isEnabled = x.IsEnabled
                })
                .ToListAsync();

            return Success("조회되었습니다.", rows);
        } catch (Exception ex) {
            return ErrorData(GetErrorMessage(ex));
        }
    }

    [HttpPost, ActionName("save")]
    public async Task<IActionResult> Save([FromBody] JsonElement values) {
        // 배열(JsonElement) -> List<OpcTag> 바로 맵핑
        var models = WebFlexModelMapper.PopulateDTOModel<List<OpcTag>>(values);

        if (models.Count == 0) {
            return ErrorData("저장할 노드를 선택해 주세요.");
        }

        var nodeItems = models
            .Where(x => !string.IsNullOrWhiteSpace(x.NODE_ID))
            .ToList();

        if (nodeItems.Count == 0) {
            return ErrorData("저장할 수 있는 노드가 없습니다.");
        }

        // nodes 배열의 각 항목에 deviceId가 포함되어 있어야 함 (모든 항목 동일한 디바이스 기준)
        var deviceId = nodeItems
            .Select(x => x.DEVICE_ID)
            .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));

        if (string.IsNullOrWhiteSpace(deviceId)) {
            return ErrorData("디바이스를 선택해 주세요.");
        }

        await using var tran = await _db.Database.BeginTransactionAsync();

        try {
            var device = await _db.Set<OpcDevice>()
                .FirstOrDefaultAsync(x => x.ID == deviceId);

            if (device == null) {
                throw new WebFlexMessageException("디바이스를 찾을 수 없습니다.");
            }

            var group = await GetOrCreateDeviceGroupAsync(device);

            var now = DateTime.UtcNow;
            var insertCount = 0;
            var skipCount = 0;

            var nextSortOrder = await CreateNextSortOrderAsync(deviceId);
            var tagIds = await _newNo.NewNosAsync("GT", nodeItems.Count);
            var tagIdIndex = 0;

            var currentValues = new List<CurrentValue>();

            foreach (var tag in nodeItems) {
                var exists = await _db.Set<OpcTag>()
                    .AnyAsync(x => x.DEVICE_ID == deviceId && x.NODE_ID == tag.NODE_ID);

                if (exists) {
                    skipCount++;
                    continue;
                }

                tag.ID = tagIds[tagIdIndex++];
                tag.DEVICE_ID = deviceId;
                tag.GROUP_ID = group.ID;
                tag.TAG_NAME = string.IsNullOrWhiteSpace(tag.TAG_NAME) ? tag.NODE_ID : tag.TAG_NAME;
                tag.PROTECT_TYPE = NormalizeProtectType(tag.PROTECT_TYPE);
                tag.IS_COLLECTENABLED = true;
                tag.SAVE_TO_DATABASE = true;
                tag.SHOW_ON_DASHBOARD = false;
                tag.SAMPLINGINTERVALMS = device.SAMPLINGINTERVALMS ?? 1000;
                tag.SORT_ORDER = nextSortOrder++;
                tag.IsEnabled = true;
                tag.CreatedAt = now;
                tag.UpdatedAt = now;

                _db.Set<OpcTag>().Add(tag);

                currentValues.Add(new CurrentValue {
                    TAG_ID = tag.ID,
                    GROUP_ID = group.ID,
                    STATUS = VaribaleStatusType.Bad,
                    VALUE = null,
                    COOKIE_VALUE = null,
                    UPDATE_COUNT = 0,
                    SOURCETIMESTAMP = null,
                    RECEIVEDAT = now,
                    UPDATEDAT = now
                });

                insertCount++;
            }

            await _db.SaveChangesAsync();
            await tran.CommitAsync();

            if (currentValues.Count > 0) {
                await InsertCurrentValuesAsync(currentValues);
            }

            return Success($"저장 완료: 신규 {insertCount}개, 중복 제외 {skipCount}개", new {
                insertCount,
                skipCount,
                groupId = group.ID,
                groupName = group.GROUP_NAME
            });
        } catch (WebFlexMessageException ex) {
            await tran.RollbackAsync();
            return ErrorData(ex.Message);
        } catch (Exception ex) {
            await tran.RollbackAsync();
            return ErrorData(GetErrorMessage(ex));
        }
    }

    [HttpPost, ActionName("update")]
    public async Task<IActionResult> Update([FromBody] JsonElement request) {
        var model = WebFlexModelMapper.PopulateDTOModel<OpcTag>(request);

        if (string.IsNullOrWhiteSpace(model.ID)) {
            return ErrorData("태그 아이디가 없습니다.");
        }

        await using var tran = await _db.Database.BeginTransactionAsync();

        try {
            var tag = await _db.Set<OpcTag>()
                .FirstOrDefaultAsync(x => x.ID == model.ID);

            if (tag == null) {
                throw new WebFlexMessageException("태그를 찾을 수 없습니다.");
            }

            var originalId = tag.ID;
            var originalDeviceId = tag.DEVICE_ID;

            WebFlexModelMapper.ApplyModel(tag, request, NormalizeOpcTag);

            tag.ID = originalId;

            if (string.IsNullOrWhiteSpace(tag.DEVICE_ID)) {
                tag.DEVICE_ID = originalDeviceId;
            }

            if (string.IsNullOrWhiteSpace(tag.TAG_NAME)) {
                throw new WebFlexMessageException("태그명을 입력해 주세요.");
            }

            if (string.IsNullOrWhiteSpace(tag.NODE_ID)) {
                throw new WebFlexMessageException("NodeId를 입력해 주세요.");
            }

            tag.SAMPLINGINTERVALMS = tag.SAMPLINGINTERVALMS <= 0 ? null : tag.SAMPLINGINTERVALMS;
            tag.SORT_ORDER = tag.SORT_ORDER <= 0 ? null : tag.SORT_ORDER;
            tag.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            await tran.CommitAsync();

            var updatedAt = DateTime.UtcNow;

            await _tsdDb.Set<CurrentValue>()
                .Where(x => x.TAG_ID == tag.ID)
                .ExecuteUpdateAsync(x => x
                    .SetProperty(v => v.GROUP_ID, tag.GROUP_ID)
                    .SetProperty(v => v.UPDATEDAT, updatedAt)
                );

            return Success("태그 정보가 수정되었습니다.");
        } catch (WebFlexMessageException ex) {
            await tran.RollbackAsync();
            return ErrorData(ex.Message);
        } catch (Exception ex) {
            await tran.RollbackAsync();
            return ErrorData(GetErrorMessage(ex));
        }
    }

    [ApiExplorerSettings(IgnoreApi = true)]
    [HttpPost, ActionName("runtest")]
    public IActionResult RunTest([FromBody] JsonElement request) {
        try {
            var model = WebFlexModelMapper.PopulateDTOModel<DeviceTagRunTestModel>(request);

            var expression = model.EXPRESSIONS;
            var dataType = model.DATA_TYPE;
            var raw = model.RAW ?? model.VALUE;

            if (string.IsNullOrWhiteSpace(expression)) {
                return ErrorData("상세설정을 입력해 주세요.");
            }

            var interpreter = new Interpreter();
            var rawValue = ConvertTestValue(dataType, raw);
            var rawType = GetRawType(dataType);

            var parseExpression = interpreter.Parse(
                expression,
                new Parameter("raw", rawType)
            );

            var result = parseExpression.Invoke(rawValue);

            return Success("테스트가 완료되었습니다.", result);
        } catch (WebFlexMessageException ex) {
            return ErrorData(ex.Message);
        } catch (Exception ex) {
            return ErrorData(GetErrorMessage(ex));
        }
    }

    [HttpPost, ActionName("delete")]
    public async Task<IActionResult> Delete([FromBody] JsonElement request) {
        try {
            var models = WebFlexModelMapper.PopulateDTOModel<List<OpcTag>>(request);

            if (models.Count == 0) {
                return ErrorData("삭제할 태그를 선택해 주세요.");
            }

            var ids = models
                .Select(x => x.ID)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

            if (ids.Count == 0) {
                return ErrorData("삭제할 태그를 선택해 주세요.");
            }

            var tags = await _db.Set<OpcTag>()
                .Where(x => ids.Contains(x.ID))
                .ToListAsync();

            if (tags.Count == 0) {
                return ErrorData("삭제할 태그를 찾을 수 없습니다.");
            }

            var tagIds = tags.Select(x => x.ID).ToList();

            await _tsdDb.Set<CurrentValue>()
                .Where(x => tagIds.Contains(x.TAG_ID))
                .ExecuteDeleteAsync();

            _db.Set<OpcTag>().RemoveRange(tags);
            await _db.SaveChangesAsync();

            return Success($"{tags.Count}개의 태그가 삭제되었습니다.");
        } catch (Exception ex) {
            return ErrorData(GetErrorMessage(ex));
        }
    }

    private class DeviceTagRunTestModel : OpcTag {
        public string? RAW { get; set; }
        public string? VALUE { get; set; }
    }

    private static void NormalizeOpcTag(OpcTag tag) {
        tag.GROUP_ID = string.IsNullOrWhiteSpace(tag.GROUP_ID) ? null : tag.GROUP_ID;
        tag.PROTECT_TYPE = NormalizeProtectType(tag.PROTECT_TYPE);
    }

    private static string NormalizeProtectType(string? value) {
        return value switch {
            "ReadOnly" => "ReadOnly",
            "ReadWrite" => "ReadWrite",
            "WriteOnly" => "WriteOnly",
            "READ_ONLY" => "ReadOnly",
            "READ_WRITE" => "ReadWrite",
            "WRITE_ONLY" => "WriteOnly",
            "읽기 전용" => "ReadOnly",
            "읽고 쓰기" => "ReadWrite",
            "쓰기 전용" => "WriteOnly",
            _ => "ReadOnly"
        };
    }

    private async Task<OpcGroup> GetOrCreateDeviceGroupAsync(OpcDevice device) {
        var group = await _db.Set<OpcGroup>()
            .FirstOrDefaultAsync(x => x.GROUP_NAME == device.DEVICE_NAME);

        if (group != null) {
            return group;
        }

        var now = DateTime.UtcNow;

        group = new OpcGroup {
            ID = await CreateGroupIdAsync(),
            GROUP_NAME = device.DEVICE_NAME,
            SORT_ORDER = await CreateGroupSortOrderAsync(),
            DESCRIPTION = $"{device.DEVICE_NAME} 자동 생성 그룹",
            IsEnabled = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.Set<OpcGroup>().Add(group);

        return group;
    }

    private async Task<string> CreateGroupIdAsync() {
        var ids = await _db.Set<OpcGroup>()
            .AsNoTracking()
            .Select(x => x.ID)
            .ToListAsync();

        var max = ids
            .Where(x => !string.IsNullOrWhiteSpace(x) && x.StartsWith("GRP"))
            .Select(x => x.Replace("GRP", ""))
            .Where(x => int.TryParse(x, out _))
            .Select(int.Parse)
            .DefaultIfEmpty(0)
            .Max();

        return $"GRP{max + 1:0000}";
    }

    private async Task<int> CreateGroupSortOrderAsync() {
        var values = await _db.Set<OpcGroup>()
            .AsNoTracking()
            .Select(x => x.SORT_ORDER)
            .ToListAsync();

        return values
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .DefaultIfEmpty(0)
            .Max() + 1;
    }

    private async Task<int> CreateNextSortOrderAsync(string deviceId) {
        var values = await _db.Set<OpcTag>()
            .AsNoTracking()
            .Where(x => x.DEVICE_ID == deviceId)
            .Select(x => x.SORT_ORDER)
            .ToListAsync();

        return values
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .DefaultIfEmpty(0)
            .Max() + 1;
    }

    private async Task InsertCurrentValuesAsync(List<CurrentValue> currentValues) {
        var tagIds = currentValues.Select(x => x.TAG_ID).ToList();

        var existsTagIds = await _tsdDb.Set<CurrentValue>()
            .Where(x => tagIds.Contains(x.TAG_ID))
            .Select(x => x.TAG_ID)
            .ToListAsync();

        var insertRows = currentValues
            .Where(x => !existsTagIds.Contains(x.TAG_ID))
            .ToList();

        if (insertRows.Count == 0) {
            return;
        }

        _tsdDb.Set<CurrentValue>().AddRange(insertRows);
        await _tsdDb.SaveChangesAsync();
    }

    private static object ConvertTestValue(string? dataType, string? raw) {
        var value = raw?.Trim() ?? "";
        var type = NormalizeDataType(dataType);

        if (value.Length == 0) {
            return type switch {
                "bool" or "bit" => false,
                "ascii" or "utf8" or "string" => "",
                "datetime" => DateTime.MinValue,
                _ => 0d
            };
        }

        try {
            return type switch {
                "bit" => ConvertToBoolean(value),
                "bool" => ConvertToBoolean(value),
                "uint8" => byte.Parse(value, CultureInfo.InvariantCulture),
                "int8" => sbyte.Parse(value, CultureInfo.InvariantCulture),
                "uint16" => ushort.Parse(value, CultureInfo.InvariantCulture),
                "int16" => short.Parse(value, CultureInfo.InvariantCulture),
                "bcd16" => int.Parse(value, CultureInfo.InvariantCulture),
                "uint32" => uint.Parse(value, CultureInfo.InvariantCulture),
                "int32" => int.Parse(value, CultureInfo.InvariantCulture),
                "bcd32" => long.Parse(value, CultureInfo.InvariantCulture),
                "uint64" => ulong.Parse(value, CultureInfo.InvariantCulture),
                "int64" => long.Parse(value, CultureInfo.InvariantCulture),
                "float" => float.Parse(value, CultureInfo.InvariantCulture),
                "double" => double.Parse(value, CultureInfo.InvariantCulture),
                "datetime" => DateTime.Parse(value, CultureInfo.InvariantCulture),
                "timestamp(ms)" => long.Parse(value, CultureInfo.InvariantCulture),
                "timestamp(s)" => long.Parse(value, CultureInfo.InvariantCulture),
                "ascii" => value,
                "utf8" => value,
                "string" => value,
                _ => double.Parse(value, CultureInfo.InvariantCulture)
            };
        } catch {
            throw new WebFlexMessageException($"테스트 값을 {type} 타입으로 변환할 수 없습니다.");
        }
    }

    private static Type GetRawType(string? dataType) {
        var type = NormalizeDataType(dataType);

        return type switch {
            "bit" => typeof(bool),
            "bool" => typeof(bool),
            "uint8" => typeof(byte),
            "int8" => typeof(sbyte),
            "uint16" => typeof(ushort),
            "int16" => typeof(short),
            "bcd16" => typeof(int),
            "uint32" => typeof(uint),
            "int32" => typeof(int),
            "bcd32" => typeof(long),
            "uint64" => typeof(ulong),
            "int64" => typeof(long),
            "float" => typeof(float),
            "double" => typeof(double),
            "datetime" => typeof(DateTime),
            "timestamp(ms)" => typeof(long),
            "timestamp(s)" => typeof(long),
            "ascii" => typeof(string),
            "utf8" => typeof(string),
            "string" => typeof(string),
            _ => typeof(double)
        };
    }

    private static string NormalizeDataType(string? dataType) {
        return (dataType ?? "")
            .Trim()
            .Replace(" ", "")
            .ToLowerInvariant();
    }

    private static bool ConvertToBoolean(string value) {
        if (string.Equals(value, "1", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "y", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase)) {
            return true;
        }

        if (string.Equals(value, "0", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "false", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "n", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "no", StringComparison.OrdinalIgnoreCase)) {
            return false;
        }

        throw new WebFlexMessageException("bool 타입 테스트 값은 true/false 또는 1/0으로 입력해 주세요.");
    }
}