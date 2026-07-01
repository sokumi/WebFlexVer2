using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebFlex.Shared;
using WebFlex.UI.Common;
using WebFlex.UI.Data;

namespace WebFlex.UI.Controllers;

[Route("main/card/[action]")]
public class MainCardController : WebFlexApiController {
    private readonly WebFlexDbContext _db;
    private readonly TsdReadDbContext _tsdDb;

    public MainCardController(WebFlexDbContext db, TsdReadDbContext tsdDb) {
        _db = db;
        _tsdDb = tsdDb;
    }

    [HttpGet, ActionName("list")]
    public async Task<IActionResult> List() {
        try {
            var groups = await _db.Set<OpcGroup>()
                .AsNoTracking()
                .Include(x => x.MajorGroup)
                .ToListAsync();

            var groupIds = groups.Select(x => x.ID).ToList();

            var tags = await _db.Set<OpcTag>()
                .AsNoTracking()
                .Where(x => x.GROUP_ID != null && groupIds.Contains(x.GROUP_ID))
                .ToListAsync();

            var tagIds = tags.Select(x => x.ID).Distinct().ToList();

            var currentValues = await _tsdDb.Set<CurrentValue>()
                .AsNoTracking()
                .Where(x => tagIds.Contains(x.TAG_ID))
                .ToListAsync();

            var currentMap = currentValues
                .GroupBy(x => x.TAG_ID)
                .ToDictionary(x => x.Key, x => x.First());

            var cards = groups
                .OrderBy(x => x.MajorGroup == null ? int.MaxValue : x.MajorGroup.SORT_ORDER ?? int.MaxValue)
                .ThenBy(x => x.SORT_ORDER ?? int.MaxValue)
                .ThenBy(x => x.GROUP_NAME)
                .Select(group => {
                    var groupTags = tags
                        .Where(x => x.GROUP_ID == group.ID)
                        .OrderBy(x => x.SORT_ORDER ?? int.MaxValue)
                        .ThenBy(x => x.TAG_NAME)
                        .ToList();

                    var dashboardTags = groupTags
                        .Where(x => x.SHOW_ON_DASHBOARD)
                        .ToList();

                    var tagRows = dashboardTags.Select(tag => {
                        currentMap.TryGetValue(tag.ID, out var current);

                        var state = ResolveDashboardState(current);

                        return new {
                            tagId = tag.ID,
                            tagName = tag.TAG_NAME,
                            nodeId = tag.NODE_ID,
                            description = string.IsNullOrWhiteSpace(tag.DESCRIPTION)
                                ? tag.TAG_NAME ?? tag.NODE_ID
                                : tag.DESCRIPTION,
                            value = current?.COOKIE_VALUE ?? current?.VALUE,
                            rawValue = current?.VALUE,
                            cookieValue = current?.COOKIE_VALUE,
                            status = current?.STATUS?.ToString(),
                            state,
                            sortOrder = tag.SORT_ORDER
                        };
                    }).ToList();

                    var connectedCount = groupTags.Count(tag =>
                        currentMap.TryGetValue(tag.ID, out var current) &&
                        current.STATUS == VaribaleStatusType.Good);

                    var totalCount = groupTags.Count;
                    var disconnectedCount = totalCount - connectedCount;

                    var cardState = ResolveCardState(
                        tagRows.Select(x => x.state).ToList(),
                        totalCount,
                        connectedCount
                    );

                    return new {
                        groupId = group.ID,
                        majorGroupId = group.MAJOR_GROUP_ID,
                        majorGroupName = group.MajorGroup?.MAJOR_GROUP_NAME,
                        groupName = group.GROUP_NAME,
                        description = group.DESCRIPTION,
                        identityText = string.IsNullOrWhiteSpace(group.MAJOR_GROUP_ID)
                            ? group.ID
                            : $"{group.MAJOR_GROUP_ID} / {group.ID}",
                        state = cardState,
                        stateText = GetStateText(cardState),
                        footerText = GetFooterText(cardState),
                        totalCount,
                        connectedCount,
                        disconnectedCount,
                        tags = tagRows
                    };
                })
                .ToList();

            return Success("조회되었습니다.", cards);
        } catch (Exception ex) {
            return ErrorData(GetErrorMessage(ex));
        }
    }

    private static string ResolveDashboardState(CurrentValue? current) {
        if (current == null) return "gray";
        if (current.STATUS != VaribaleStatusType.Good) return "red";
        return "green";
    }

    private static string ResolveCardState(List<string> states, int totalCount, int connectedCount) {
        if (totalCount == 0) return "gray";
        if (connectedCount == 0) return "gray";
        if (states.Count == 0) return "green";

        return states
            .OrderBy(GetStatePriority)
            .First();
    }

    private static int GetStatePriority(string state) {
        return state switch {
            "gray" => 0,
            "flashRed" => 1,
            "red" => 2,
            "orange" => 3,
            "green" => 4,
            _ => 9
        };
    }

    private static string GetStateText(string state) {
        return state switch {
            "gray" => "휴면",
            "flashRed" => "위험",
            "red" => "점검 필요",
            "orange" => "주의",
            "green" => "정상",
            _ => "확인"
        };
    }

    private static string GetFooterText(string state) {
        return state switch {
            "gray" => "설비가 가동 중이 아닙니다",
            "flashRed" => "즉시 점검이 필요한 태그가 있습니다",
            "red" => "점검이 필요한 태그가 있습니다",
            "orange" => "주의가 필요한 태그가 있습니다",
            "green" => "모든 태그 정상 작동 중",
            _ => "상태 확인이 필요합니다"
        };
    }
}