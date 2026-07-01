using System.Text.Json;
using System.Threading.Channels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebFlex.Shared;
using WebFlex.UI.Data;
using WebFlex.UI.Services;

namespace WebFlex.UI.Controllers;

[ApiController]
[Route("main/list")]
public class MainListController : ControllerBase {
    private readonly TsdReadDbContext _db;
    private readonly WebFlexDbContext _webFlexDb;
    private readonly CurrentValueNotifyService _notifyService;

    public MainListController(
        TsdReadDbContext db,
        WebFlexDbContext webFlexDb,
        CurrentValueNotifyService notifyService) {
        _db = db;
        _webFlexDb = webFlexDb;
        _notifyService = notifyService;
    }

    [HttpGet("page")]
    public async Task<IActionResult> Page(
        int skip = 0,
        int take = 100,
        string? groupId = null,
        string? keyword = null,
        CancellationToken cancellationToken = default) {
        if (skip < 0) {
            skip = 0;
        }

        if (take <= 0) {
            take = 100;
        }

        if (take > 300) {
            take = 300;
        }

        var query = _db.Set<CurrentValue>()
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(groupId)) {
            query = query.Where(x => x.GROUP_ID == groupId);
        }

        if (!string.IsNullOrWhiteSpace(keyword)) {
            var q = keyword.Trim();

            var matchedTagIdsByDescription = await _webFlexDb.Set<OpcTag>()
                .AsNoTracking()
                .Where(x =>
                    x.DESCRIPTION != null &&
                    x.DESCRIPTION.Contains(q))
                .Select(x => x.ID)
                .ToListAsync(cancellationToken);

            query = query.Where(x =>
                x.TAG_ID.Contains(q) ||
                (x.GROUP_ID != null && x.GROUP_ID.Contains(q)) ||
                (x.VALUE != null && x.VALUE.Contains(q)) ||
                (x.COOKIE_VALUE != null && x.COOKIE_VALUE.Contains(q)) ||
                matchedTagIdsByDescription.Contains(x.TAG_ID));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var currentRows = await query
            .OrderBy(x => x.GROUP_ID)
            .ThenBy(x => x.TAG_ID)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        var tagIds = currentRows
            .Select(x => x.TAG_ID)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .ToList();

        var tagDescriptionMap = await _webFlexDb.Set<OpcTag>()
            .AsNoTracking()
            .Where(x => tagIds.Contains(x.ID))
            .Select(x => new {
                tagId = x.ID,
                description = x.DESCRIPTION
            })
            .ToDictionaryAsync(
                x => x.tagId,
                x => x.description,
                cancellationToken
            );

        var rows = currentRows
            .Select(x => ToRowDto(x, tagDescriptionMap))
            .ToList();

        return Ok(new {
            success = true,
            data = new {
                totalCount,
                skip,
                take,
                rows
            }
        });
    }

    [HttpGet("groups")]
    public async Task<IActionResult> Groups(CancellationToken cancellationToken = default) {
        var groups = await _db.Set<CurrentValue>()
            .AsNoTracking()
            .GroupBy(x => x.GROUP_ID)
            .Select(x => new {
                groupId = x.Key,
                count = x.Count(),
                badCount = x.Count(v => v.STATUS != VaribaleStatusType.Good)
            })
            .OrderBy(x => x.groupId)
            .ToListAsync(cancellationToken);

        var groupIds = groups
            .Select(x => x.groupId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .ToList();

        var groupNameMap = await _webFlexDb.Set<OpcGroup>()
            .AsNoTracking()
            .Where(x => groupIds.Contains(x.ID))
            .Select(x => new {
                groupId = x.ID,
                groupName = x.GROUP_NAME
            })
            .ToDictionaryAsync(
                x => x.groupId,
                x => x.groupName,
                cancellationToken
            );

        var rows = groups
            .Select(x => new {
                x.groupId,
                groupName = x.groupId != null && groupNameMap.TryGetValue(x.groupId, out var groupName)
                    ? groupName
                    : x.groupId,
                x.count,
                x.badCount
            })
            .ToList();

        return Ok(new {
            success = true,
            data = rows
        });
    }
     
    [HttpGet("stream")]
    public async Task Stream(CancellationToken cancellationToken) {
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";
        Response.Headers.ContentType = "text/event-stream";

        var channel = Channel.CreateUnbounded<string>();
        var clientId = _notifyService.Subscribe(channel);

        try {
            await WriteEventAsync("connected", "{}", cancellationToken);

            await foreach (var payload in channel.Reader.ReadAllAsync(cancellationToken)) {
                var row = ConvertNotifyPayload(payload);

                if (row == null) {
                    continue;
                }

                var json = JsonSerializer.Serialize(
                    row,
                    new JsonSerializerOptions {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });

                await WriteEventAsync("currentvalue", json, cancellationToken);
            }
        } finally {
            _notifyService.Unsubscribe(clientId);
        }
    }

    private static CurrentValueRowDto ToRowDto(
        CurrentValue row,
        IReadOnlyDictionary<string, string?> tagDescriptionMap) {
        tagDescriptionMap.TryGetValue(row.TAG_ID, out var description);

        return new CurrentValueRowDto {
            GroupId = row.GROUP_ID,
            TagId = row.TAG_ID,
            CollectionSetting = description,
            Value = row.VALUE,
            CookieValue = row.COOKIE_VALUE,
            Status = row.STATUS.HasValue ? (int?)row.STATUS.Value : null,
            UpdateCount = row.UPDATE_COUNT,
            SourceTimestamp = row.SOURCETIMESTAMP,
            ReceivedAt = row.RECEIVEDAT,
            UpdatedAt = row.UPDATEDAT
        };
    }

    private static CurrentValueRowDto? ConvertNotifyPayload(string payload) {
        try {
            using var document = JsonDocument.Parse(payload);
            var root = document.RootElement;

            var tagId = GetString(root, "tag_id", "TAG_ID", "tagId");

            if (string.IsNullOrWhiteSpace(tagId)) {
                return null;
            }

            return new CurrentValueRowDto {
                GroupId = GetString(root, "group_id", "GROUP_ID", "groupId"),
                TagId = tagId,
                CollectionSetting = null,
                Value = GetString(root, "value", "VALUE"),
                CookieValue = GetString(root, "cookie_value", "COOKIE_VALUE", "cookieValue"),
                Status = GetInt(root, "status", "STATUS"),
                UpdateCount = GetInt(root, "update_count", "UPDATE_COUNT", "updateCount"),
                SourceTimestamp = GetDateTime(root, "source_timestamp", "SOURCETIMESTAMP", "sourceTimestamp"),
                ReceivedAt = GetDateTime(root, "received_at", "RECEIVEDAT", "receivedAt") ?? DateTime.UtcNow,
                UpdatedAt = GetDateTime(root, "updated_at", "UPDATEDAT", "updatedAt") ?? DateTime.UtcNow
            };
        } catch {
            return null;
        }
    }

    private async Task WriteEventAsync(
        string eventName,
        string data,
        CancellationToken cancellationToken) {
        await Response.WriteAsync($"event: {eventName}\n", cancellationToken);
        await Response.WriteAsync($"data: {data}\n\n", cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);
    }

    private static string? GetString(JsonElement root, params string[] names) {
        if (!TryGetProperty(root, out var property, names)) {
            return null;
        }

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

    private static int? GetInt(JsonElement root, params string[] names) {
        if (!TryGetProperty(root, out var property, names)) {
            return null;
        }

        if (property.ValueKind == JsonValueKind.Number &&
            property.TryGetInt32(out var number)) {
            return number;
        }

        var text = GetString(root, names);

        return int.TryParse(text, out var value)
            ? value
            : null;
    }

    private static DateTime? GetDateTime(JsonElement root, params string[] names) {
        var text = GetString(root, names);

        if (string.IsNullOrWhiteSpace(text)) {
            return null;
        }

        return DateTime.TryParse(text, out var value)
            ? DateTime.SpecifyKind(value, DateTimeKind.Utc)
            : null;
    }

    private static bool TryGetProperty(
        JsonElement root,
        out JsonElement property,
        params string[] names) {
        property = default;

        if (root.ValueKind != JsonValueKind.Object) {
            return false;
        }

        var targetNames = names
            .Select(NormalizeName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var item in root.EnumerateObject()) {
            if (targetNames.Contains(NormalizeName(item.Name))) {
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

    private sealed class CurrentValueRowDto {
        public string? GroupId { get; set; }

        public string TagId { get; set; } = "";

        /// <summary>
        /// opc_tag.description
        /// </summary>
        public string? CollectionSetting { get; set; }

        /// <summary>
        /// currentvalue.value
        /// </summary>
        public string? Value { get; set; }

        /// <summary>
        /// currentvalue.cookie_value
        /// </summary>
        public string? CookieValue { get; set; }

        public int? Status { get; set; }

        public int? UpdateCount { get; set; }

        public DateTime? SourceTimestamp { get; set; }

        public DateTime ReceivedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}