using DynamicExpresso;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text.Json;
using WebFlex.Shared;
using WebFlex.Shared.Exceptions;
using WebFlex.UI.Data;
using WebFlex.UI.Services;

namespace WebFlex.UI.Controllers.Device;

[Route("device/tag/[action]")]
public class DeviceTagController : Controller {
    private readonly WebFlexDbContext _db;
    private readonly TsdReadDbContext _tsdDb;
    private readonly OpcBrowseService _opcBrowseService;
    private readonly INewNoService _newNo;

    public DeviceTagController(
        WebFlexDbContext db,
        TsdReadDbContext tsdDb,
        OpcBrowseService opcBrowseService,
        INewNoService newNo,
        IConfiguration configuration) {
        _db = db;
        _tsdDb = tsdDb;
        _opcBrowseService = opcBrowseService;
        _newNo = newNo;
    }

    private IActionResult Success(string message = "처리되었습니다.", object? data = null) {
        return Json(new {
            success = true,
            message,
            data
        });
    }

    private IActionResult ErrorData(string message, object? data = null) {
        return Json(new {
            success = false,
            message,
            data
        });
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

        return Success("조회되었습니다.", rows);
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

        return Success("조회되었습니다.", new {
            deviceCount,
            nodeCount = 0,
            variableNodeCount = 0,
            tagCount,
            collectTagCount
        });
    }

    [HttpGet, ActionName("check-connection")]
    public async Task<IActionResult> CheckConnection(
        string deviceId,
        CancellationToken cancellationToken = default) {
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
    }

    [HttpGet, ActionName("browse")]
    public async Task<IActionResult> Browse(
        string deviceId,
        bool onlyCollectable = true,
        CancellationToken cancellationToken = default) {
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

        try {
            var nodes = await _opcBrowseService.BrowseAsync(
                device,
                onlyCollectable,
                cancellationToken
            );

            return Success("OPC 노드를 조회했습니다.", nodes);
        } catch (Exception ex) {
            return ErrorData($"OPC 노드 조회 중 오류가 발생했습니다. {ex.Message}");
        }
    }

    [HttpGet, ActionName("list")]
    public async Task<IActionResult> List(string deviceId, string? keyword = null, bool onlyCollect = false) {
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
                (x.DESCRIPTION != null && x.DESCRIPTION.Contains(keyword))
            );
        }

        if (onlyCollect) {
            query = query.Where(x => x.IS_COLLECTENABLED);
        }

        var rows = await query
            .OrderBy(x => x.SORT_ORDER)
            .ThenBy(x => x.TAG_NAME)
            .ToListAsync();

        return Success("조회되었습니다.", rows);
    }

    [HttpPost, ActionName("save")]
    public async Task<IActionResult> Save([FromBody] JsonElement request) {
        var deviceId = GetString(request, "deviceId", "DEVICE_ID");

        if (string.IsNullOrWhiteSpace(deviceId)) {
            return ErrorData("디바이스를 선택해 주세요.");
        }

        if (!TryGetProperty(request, out var nodesElement, "nodes", "Nodes") ||
            nodesElement.ValueKind != JsonValueKind.Array ||
            nodesElement.GetArrayLength() == 0) {
            return ErrorData("저장할 노드를 선택해 주세요.");
        }

        await using var tran = await _db.Database.BeginTransactionAsync();

        try {
            var device = await _db.Set<OpcDevice>()
                .FirstOrDefaultAsync(x => x.ID == deviceId);

            if (device == null) {
                return ErrorData("디바이스를 찾을 수 없습니다.");
            }

            var group = await GetOrCreateDeviceGroupAsync(device);

            var nodeItems = nodesElement
                .EnumerateArray()
                .Where(x => string.Equals(GetString(x, "nodeClass", "NODE_CLASS"), "Variable", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (nodeItems.Count == 0) {
                return ErrorData("Variable 노드만 태그로 저장할 수 있습니다.");
            }

            var now = DateTime.UtcNow;
            var insertCount = 0;
            var skipCount = 0;

            var nextSortOrder = await CreateNextSortOrderAsync(deviceId);
            var tagIds = await _newNo.NewNosAsync("GT", nodeItems.Count);
            var tagIdIndex = 0;

            var currentValues = new List<CurrentValue>();

            foreach (var nodeItem in nodeItems) {
                var nodeId = GetString(nodeItem, "nodeId", "NODE_ID");

                if (string.IsNullOrWhiteSpace(nodeId)) {
                    skipCount++;
                    continue;
                }

                var exists = await _db.Set<OpcTag>()
                    .AnyAsync(x => x.DEVICE_ID == deviceId && x.NODE_ID == nodeId);

                if (exists) {
                    skipCount++;
                    continue;
                }

                var tag = CreateOpcTagFromJson(nodeItem);

                tag.ID = tagIds[tagIdIndex++];
                tag.DEVICE_ID = deviceId;
                tag.GROUP_ID = group.ID;
                tag.NODE_ID = nodeId;
                tag.TAG_NAME = string.IsNullOrWhiteSpace(tag.TAG_NAME) ? nodeId : tag.TAG_NAME;
                tag.PROTECT_TYPE = NormalizeProtectType(tag.PROTECT_TYPE);
                tag.SAMPLINGINTERVALMS = device.SAMPLINGINTERVALMS ?? 1000;
                tag.SORT_ORDER = nextSortOrder++;
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
        } catch (Exception ex) {
            await tran.RollbackAsync();
            return ErrorData(ex.InnerException?.Message ?? ex.Message);
        }
    }

    [HttpPost, ActionName("update")]
    public async Task<IActionResult> Update([FromBody] JsonElement request) {
        var id = GetString(request, "id", "ID");

        if (string.IsNullOrWhiteSpace(id)) {
            return ErrorData("태그 아이디가 없습니다.");
        }

        await using var tran = await _db.Database.BeginTransactionAsync();

        try {
            var tag = await _db.Set<OpcTag>()
                .FirstOrDefaultAsync(x => x.ID == id);

            if (tag == null) {
                throw new WebFlexMessageException("태그를 찾을 수 없습니다.");
            }

            ApplyOpcTagFromJson(tag, request);

            if (string.IsNullOrWhiteSpace(tag.TAG_NAME)) {
                throw new WebFlexMessageException("태그명을 입력해 주세요.");
            }

            if (string.IsNullOrWhiteSpace(tag.DEVICE_ID)) {
                tag.DEVICE_ID = GetString(request, "deviceId", "DEVICE_ID");
            }

            if (string.IsNullOrWhiteSpace(tag.NODE_ID)) {
                throw new WebFlexMessageException("NodeId를 입력해 주세요.");
            }

            tag.PROTECT_TYPE = NormalizeProtectType(tag.PROTECT_TYPE);
            tag.SAMPLINGINTERVALMS = tag.SAMPLINGINTERVALMS <= 0 ? null : tag.SAMPLINGINTERVALMS;
            tag.SORT_ORDER = tag.SORT_ORDER <= 0 ? null : tag.SORT_ORDER;
            tag.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            await tran.CommitAsync();

            await _tsdDb.Set<CurrentValue>()
                .Where(x => x.TAG_ID == tag.ID)
                .ExecuteUpdateAsync(x => x
                    .SetProperty(v => v.GROUP_ID, tag.GROUP_ID)
                    .SetProperty(v => v.UPDATEDAT, DateTime.UtcNow)
                );

            return Success("태그 정보가 수정되었습니다.");
        } catch (WebFlexMessageException ex) {
            await tran.RollbackAsync();
            return ErrorData(ex.Message);
        } catch (Exception ex) {
            await tran.RollbackAsync();
            return ErrorData(ex.InnerException?.Message ?? ex.Message);
        }
    }

    [ApiExplorerSettings(IgnoreApi = true)]
    [HttpPost, ActionName("runtest")]
    public IActionResult RunTest([FromBody] JsonElement request) {
        var expression = GetString(request, "expression", "expressions", "EXPRESSIONS");
        var dataType = GetString(request, "dataType", "DATA_TYPE");
        var raw = GetString(request, "raw", "value", "VALUE");

        if (string.IsNullOrWhiteSpace(expression)) {
            return ErrorData("계산식을 입력해 주세요.");
        }

        try {
            var interpreter = new Interpreter();

            object rawValue = ConvertTestValue(dataType, raw);
            var rawType = GetRawType(dataType);

            var parseExpression = interpreter.Parse(
                expression,
                new Parameter("raw", rawType)
            );

            var result = parseExpression.Invoke(rawValue);

            return Success("계산식 테스트가 완료되었습니다.", result);
        } catch (WebFlexMessageException ex) {
            return ErrorData(ex.Message);
        } catch (Exception ex) {
            return ErrorData(ex.InnerException?.Message ?? ex.Message);
        }
    }

    [HttpPost, ActionName("delete")]
    public async Task<IActionResult> Delete([FromBody] JsonElement request) {
        var ids = GetStringList(request, "ids", "Ids", "ID", "id");

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
    }

    private static OpcTag CreateOpcTagFromJson(JsonElement element) {
        var tag = new OpcTag {
            IS_COLLECTENABLED = true,
            SAVE_TO_DATABASE = true,
            SHOW_ON_DASHBOARD = false,
            IsEnabled = true
        };

        ApplyOpcTagFromJson(tag, element);

        return tag;
    }

    private static void ApplyOpcTagFromJson(OpcTag tag, JsonElement element) {
        SetString(element, value => tag.ID = value, "id", "ID", "tagId", "TAG_ID");
        SetString(element, value => tag.DEVICE_ID = value, "deviceId", "DEVICE_ID");
        SetString(element, value => tag.GROUP_ID = NullIfEmpty(value), "groupId", "GROUP_ID");
        SetString(element, value => tag.NODE_ID = value, "nodeId", "NODE_ID");
        SetString(element, value => tag.TAG_NAME = value, "tagName", "TAG_NAME", "displayName");
        SetString(element, value => tag.DATA_TYPE = value, "dataType", "DATA_TYPE");
        SetString(element, value => tag.PROTECT_TYPE = value, "protectType", "PROTECT_TYPE");
        SetString(element, value => tag.EXPRESSIONS = value, "expressions", "expression", "EXPRESSIONS");
        SetString(element, value => tag.DESCRIPTION = value, "description", "DESCRIPTION");

        SetBool(element, value => tag.IS_COLLECTENABLED = value, "isCollectEnabled", "IS_COLLECTENABLED");
        SetBool(element, value => tag.SAVE_TO_DATABASE = value, "saveToDatabase", "SAVE_TO_DATABASE");
        SetBool(element, value => tag.SHOW_ON_DASHBOARD = value, "showOnDashboard", "SHOW_ON_DASHBOARD");
        SetBool(element, value => tag.IsEnabled = value, "isEnabled", "IsEnabled");

        SetInt(element, value => tag.SAMPLINGINTERVALMS = value, "samplingIntervalMs", "SAMPLINGINTERVALMS");
        SetInt(element, value => tag.SORT_ORDER = value, "sortOrder", "SORT_ORDER");
    }

    private static void SetString(JsonElement element, Action<string?> setter, params string[] names) {
        if (!TryGetProperty(element, out var property, names)) {
            return;
        }

        setter(GetStringValue(property));
    }

    private static void SetBool(JsonElement element, Action<bool> setter, params string[] names) {
        if (!TryGetProperty(element, out var property, names)) {
            return;
        }

        if (property.ValueKind == JsonValueKind.True) {
            setter(true);
            return;
        }

        if (property.ValueKind == JsonValueKind.False) {
            setter(false);
            return;
        }

        var value = GetStringValue(property);

        if (string.IsNullOrWhiteSpace(value)) {
            return;
        }

        if (bool.TryParse(value, out var boolValue)) {
            setter(boolValue);
            return;
        }

        if (value == "1") {
            setter(true);
            return;
        }

        if (value == "0") {
            setter(false);
        }
    }

    private static void SetInt(JsonElement element, Action<int?> setter, params string[] names) {
        if (!TryGetProperty(element, out var property, names)) {
            return;
        }

        if (property.ValueKind == JsonValueKind.Null) {
            setter(null);
            return;
        }

        if (property.ValueKind == JsonValueKind.Number &&
            property.TryGetInt32(out var numberValue)) {
            setter(numberValue);
            return;
        }

        var value = GetStringValue(property);

        if (string.IsNullOrWhiteSpace(value)) {
            setter(null);
            return;
        }

        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue)) {
            setter(intValue);
        }
    }

    private static string? GetString(JsonElement element, params string[] names) {
        return TryGetProperty(element, out var property, names)
            ? GetStringValue(property)
            : null;
    }

    private static string? GetStringValue(JsonElement property) {
        return property.ValueKind switch {
            JsonValueKind.String => property.GetString(),
            JsonValueKind.Number => property.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => null,
            JsonValueKind.Undefined => null,
            _ => property.GetRawText()
        };
    }

    private static List<string> GetStringList(JsonElement element, params string[] names) {
        foreach (var name in names) {
            if (!TryGetProperty(element, out var property, name)) {
                continue;
            }

            if (property.ValueKind == JsonValueKind.Array) {
                return property
                    .EnumerateArray()
                    .Select(GetStringValue)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x!)
                    .Distinct()
                    .ToList();
            }

            var value = GetStringValue(property);

            if (!string.IsNullOrWhiteSpace(value)) {
                return new List<string> { value };
            }
        }

        if (element.ValueKind == JsonValueKind.Array) {
            return element
                .EnumerateArray()
                .Select(GetStringValue)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!)
                .Distinct()
                .ToList();
        }

        return new List<string>();
    }

    private static bool TryGetProperty(JsonElement element, out JsonElement property, params string[] names) {
        property = default;

        if (element.ValueKind != JsonValueKind.Object) {
            return false;
        }

        var normalizedNames = names
            .Select(NormalizeName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var item in element.EnumerateObject()) {
            if (normalizedNames.Contains(NormalizeName(item.Name))) {
                property = item.Value;
                return true;
            }
        }

        return false;
    }

    private static string NormalizeName(string value) {
        return value
            .Replace("_", "")
            .Replace("-", "")
            .ToLowerInvariant();
    }

    private static string? NullIfEmpty(string? value) {
        return string.IsNullOrWhiteSpace(value) ? null : value;
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