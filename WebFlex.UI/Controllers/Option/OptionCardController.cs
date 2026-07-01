using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using WebFlex.Shared;
using WebFlex.Shared.Exceptions;
using WebFlex.UI.Common;
using WebFlex.UI.Data;

namespace WebFlex.UI.Controllers.Options;

[Route("option/card/[action]")]
public class OptionCardController : WebFlexController {
    private readonly WebFlexDbContext _db;

    public OptionCardController(WebFlexDbContext db) {
        _db = db;
    }

    [HttpGet, ActionName("tree")]
    public async Task<IActionResult> Tree() {
        try {
            var groups = await _db.Set<OpcGroup>()
                .AsNoTracking()
                .Include(x => x.MajorGroup)
                .OrderBy(x => x.MajorGroup == null ? int.MaxValue : x.MajorGroup.SORT_ORDER ?? int.MaxValue)
                .ThenBy(x => x.SORT_ORDER ?? int.MaxValue)
                .ThenBy(x => x.GROUP_NAME)
                .Select(x => new {
                    groupId = x.ID,
                    groupName = x.GROUP_NAME,
                    majorGroupId = x.MAJOR_GROUP_ID,
                    majorGroupName = x.MajorGroup == null ? null : x.MajorGroup.MAJOR_GROUP_NAME,
                    tagCount = _db.Set<OpcTag>().Count(t => t.GROUP_ID == x.ID),
                    dashboardTagCount = _db.Set<OpcTag>().Count(t => t.GROUP_ID == x.ID && t.SHOW_ON_DASHBOARD)
                })
                .ToListAsync();

            return Success("조회되었습니다.", groups);
        } catch (Exception ex) {
            return ErrorData(GetErrorMessage(ex));
        }
    }

    [HttpGet, ActionName("tags")]
    public async Task<IActionResult> Tags(string groupId) {
        try {
            if (string.IsNullOrWhiteSpace(groupId)) {
                return ErrorData("중그룹을 선택해 주세요.");
            }

            var rows = await _db.Set<OpcTag>()
                .AsNoTracking()
                .Where(x => x.GROUP_ID == groupId)
                .OrderByDescending(x => x.SHOW_ON_DASHBOARD)
                .ThenBy(x => x.SORT_ORDER ?? int.MaxValue)
                .ThenBy(x => x.TAG_NAME)
                .Select(x => new {
                    tagId = x.ID,
                    nodeId = x.NODE_ID,
                    tagName = x.TAG_NAME,
                    description = x.DESCRIPTION,
                    showOnDashboard = x.SHOW_ON_DASHBOARD,
                    sortOrder = x.SORT_ORDER,
                    dataType = x.DATA_TYPE,
                    isEnabled = x.IsEnabled,
                    optionCount = _db.Set<OpcCardOption>().Count(o => o.TAG_ID == x.ID)
                })
                .ToListAsync();

            return Success("조회되었습니다.", rows);
        } catch (Exception ex) {
            return ErrorData(GetErrorMessage(ex));
        }
    }

    [HttpGet, ActionName("options")]
    public async Task<IActionResult> Options(string tagId) {
        try {
            if (string.IsNullOrWhiteSpace(tagId)) {
                return ErrorData("태그를 선택해 주세요.");
            }

            var rows = await _db.Set<OpcCardOption>()
                .AsNoTracking()
                .Where(x => x.TAG_ID == tagId)
                .OrderBy(x => x.SORT_ORDER ?? int.MaxValue)
                .ThenBy(x => x.ID)
                .Select(x => new {
                    id = x.ID,
                    tagId = x.TAG_ID,
                    state = x.STATE,
                    matchType = x.MATCH_TYPE,
                    textValue = x.TEXT_VALUE,
                    minValue = x.MIN_VALUE,
                    maxValue = x.MAX_VALUE,
                    sortOrder = x.SORT_ORDER,
                    description = x.DESCRIPTION,
                    isEnabled = x.IsEnabled
                })
                .ToListAsync();

            return Success("조회되었습니다.", rows);
        } catch (Exception ex) {
            return ErrorData(GetErrorMessage(ex));
        }
    }

    [HttpPost, ActionName("save-tag")]
    public async Task<IActionResult> SaveTag([FromBody] JsonElement request) {
        var model = WebFlexModelMapper.PopulateDTOModel<OpcTag>(request);

        if (string.IsNullOrWhiteSpace(model.ID)) {
            return ErrorData("태그를 선택해 주세요.");
        }

        await using var tran = await _db.Database.BeginTransactionAsync();

        try {
            var tag = await _db.Set<OpcTag>().FirstOrDefaultAsync(x => x.ID == model.ID);

            if (tag == null) {
                throw new WebFlexMessageException("태그를 찾을 수 없습니다.");
            }

            tag.SHOW_ON_DASHBOARD = model.SHOW_ON_DASHBOARD;
            tag.SORT_ORDER = model.SORT_ORDER <= 0 ? null : model.SORT_ORDER;
            tag.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            await tran.CommitAsync();

            return Success("태그 표시 옵션이 저장되었습니다.");
        } catch (WebFlexMessageException ex) {
            await tran.RollbackAsync();
            return ErrorData(ex.Message);
        } catch (Exception ex) {
            await tran.RollbackAsync();
            return ErrorData(GetErrorMessage(ex));
        }
    }

    [HttpPost, ActionName("save-option")]
    public async Task<IActionResult> SaveOption([FromBody] JsonElement request) {
        var model = WebFlexModelMapper.PopulateDTOModel<OpcCardOption>(request);
        NormalizeOption(model);

        if (string.IsNullOrWhiteSpace(model.TAG_ID)) {
            return ErrorData("태그를 선택해 주세요.");
        }

        if (string.IsNullOrWhiteSpace(model.STATE)) {
            return ErrorData("표시 상태를 선택해 주세요.");
        }

        if (string.IsNullOrWhiteSpace(model.MATCH_TYPE)) {
            return ErrorData("조건 타입을 선택해 주세요.");
        }

        await using var tran = await _db.Database.BeginTransactionAsync();

        try {
            var tagExists = await _db.Set<OpcTag>()
                .AsNoTracking()
                .AnyAsync(x => x.ID == model.TAG_ID);

            if (!tagExists) {
                throw new WebFlexMessageException("태그를 찾을 수 없습니다.");
            }

            var now = DateTime.UtcNow;

            if (string.IsNullOrWhiteSpace(model.ID)) {
                model.ID = await CreateOptionIdAsync();
                model.IsEnabled = true;
                model.CreatedAt = now;
                model.UpdatedAt = now;

                _db.Set<OpcCardOption>().Add(model);
            } else {
                var entity = await _db.Set<OpcCardOption>()
                    .FirstOrDefaultAsync(x => x.ID == model.ID);

                if (entity == null) {
                    throw new WebFlexMessageException("옵션을 찾을 수 없습니다.");
                }

                WebFlexModelMapper.ApplyModel(entity, request, NormalizeOption);
                entity.ID = model.ID;
                entity.TAG_ID = model.TAG_ID;
                entity.UpdatedAt = now;
            }

            await _db.SaveChangesAsync();
            await tran.CommitAsync();

            return Success("저장되었습니다.");
        } catch (WebFlexMessageException ex) {
            await tran.RollbackAsync();
            return ErrorData(ex.Message);
        } catch (Exception ex) {
            await tran.RollbackAsync();
            return ErrorData(GetErrorMessage(ex));
        }
    }

    [HttpPost, ActionName("delete-option")]
    public async Task<IActionResult> DeleteOption([FromBody] JsonElement request) {
        var model = WebFlexModelMapper.PopulateDTOModel<OpcCardOption>(request);

        if (string.IsNullOrWhiteSpace(model.ID)) {
            return ErrorData("삭제할 옵션을 선택해 주세요.");
        }

        await using var tran = await _db.Database.BeginTransactionAsync();

        try {
            var entity = await _db.Set<OpcCardOption>()
                .FirstOrDefaultAsync(x => x.ID == model.ID);

            if (entity == null) {
                throw new WebFlexMessageException("옵션을 찾을 수 없습니다.");
            }

            _db.Set<OpcCardOption>().Remove(entity);
            await _db.SaveChangesAsync();
            await tran.CommitAsync();

            return Success("삭제되었습니다.");
        } catch (WebFlexMessageException ex) {
            await tran.RollbackAsync();
            return ErrorData(ex.Message);
        } catch (Exception ex) {
            await tran.RollbackAsync();
            return ErrorData(GetErrorMessage(ex));
        }
    }

    private static void NormalizeOption(OpcCardOption model) {
        model.STATE = NormalizeState(model.STATE);
        model.MATCH_TYPE = NormalizeMatchType(model.MATCH_TYPE);
        model.TEXT_VALUE = string.IsNullOrWhiteSpace(model.TEXT_VALUE) ? null : model.TEXT_VALUE.Trim();
        model.SORT_ORDER = model.SORT_ORDER <= 0 ? null : model.SORT_ORDER;
        model.DESCRIPTION = string.IsNullOrWhiteSpace(model.DESCRIPTION) ? null : model.DESCRIPTION.Trim();
    }

    private static string NormalizeState(string? value) {
        return value switch {
            "gray" => "gray",
            "flashRed" => "flashRed",
            "red" => "red",
            "orange" => "orange",
            "green" => "green",
            _ => "green"
        };
    }

    private static string NormalizeMatchType(string? value) {
        return value switch {
            "Always" => "Always",
            "Equals" => "Equals",
            "Contains" => "Contains",
            "NumberRange" => "NumberRange",
            "NumberGte" => "NumberGte",
            "NumberLte" => "NumberLte",
            "BoolEquals" => "BoolEquals",
            _ => "Equals"
        };
    }

    private async Task<string> CreateOptionIdAsync() {
        var ids = await _db.Set<OpcCardOption>()
            .AsNoTracking()
            .Select(x => x.ID)
            .ToListAsync();

        var max = ids
            .Where(x => !string.IsNullOrWhiteSpace(x) && x.StartsWith("DOPT"))
            .Select(x => x.Replace("DOPT", ""))
            .Where(x => int.TryParse(x, out _))
            .Select(int.Parse)
            .DefaultIfEmpty(0)
            .Max();

        return $"DOPT{max + 1:000000}";
    }
}