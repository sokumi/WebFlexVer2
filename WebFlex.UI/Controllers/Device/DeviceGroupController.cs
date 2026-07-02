using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using WebFlex.Shared;
using WebFlex.Shared.Exceptions;
using WebFlex.UI.Common;
using WebFlex.UI.Data;

namespace WebFlex.UI.Controllers.Device;

[Route("device/[action]")]
public class DeviceGroupController : WebFlexController {
    private readonly WebFlexDbContext _db;
    private readonly TsdReadDbContext _tsdDb;

    public DeviceGroupController(WebFlexDbContext db, TsdReadDbContext tsdDb) {
        _db = db;
        _tsdDb = tsdDb;
    }

    [HttpGet, ActionName("group-tree")]
    public async Task<IActionResult> GroupTree() {
        try {
            var majorGroups = await _db.Set<OpcMajorGroup>()
                .AsNoTracking()
                .OrderBy(x => x.SORT_ORDER ?? int.MaxValue)
                .ThenBy(x => x.MAJOR_GROUP_NAME)
                .Select(x => new {
                    id = x.ID,
                    name = x.MAJOR_GROUP_NAME,
                    description = x.DESCRIPTION,
                    sortOrder = x.SORT_ORDER,
                    groupCount = _db.Set<OpcGroup>().Count(g => g.MAJOR_GROUP_ID == x.ID),
                    tagCount = _db.Set<OpcTag>().Count(t => t.Group != null && t.Group.MAJOR_GROUP_ID == x.ID)
                })
                .ToListAsync();

            var groups = await SelectGroupRows(_db.Set<OpcGroup>().AsNoTracking()).ToListAsync();

            return Success("조회되었습니다.", new { majorGroups, groups });
        } catch (Exception ex) {
            return ErrorData(GetErrorMessage(ex));
        }
    }

    [HttpGet, ActionName("group-list")]
    public async Task<IActionResult> GroupList(string? majorGroupId = null) {
        try {
            var query = _db.Set<OpcGroup>().AsNoTracking();

            if (!string.IsNullOrWhiteSpace(majorGroupId)) {
                query = majorGroupId == "__none"
                    ? query.Where(x => x.MAJOR_GROUP_ID == null || x.MAJOR_GROUP_ID == "")
                    : query.Where(x => x.MAJOR_GROUP_ID == majorGroupId);
            }

            var rows = await SelectGroupRows(query).ToListAsync();

            return Success("조회되었습니다.", rows);
        } catch (Exception ex) {
            return ErrorData(GetErrorMessage(ex));
        }
    }

    [HttpGet, ActionName("tag-list")]
    public async Task<IActionResult> TagList(string groupId) {
        try {
            if (string.IsNullOrWhiteSpace(groupId)) {
                return ErrorData("중그룹을 선택해 주세요.");
            }

            var rows = await _db.Set<OpcTag>()
                .AsNoTracking()
                .Where(x => x.GROUP_ID == groupId)
                .OrderBy(x => x.SORT_ORDER ?? int.MaxValue)
                .ThenBy(x => x.TAG_NAME)
                .Select(x => new {
                    id = x.ID,
                    deviceId = x.DEVICE_ID,
                    groupId = x.GROUP_ID,
                    nodeId = x.NODE_ID,
                    tagName = x.TAG_NAME,
                    dataType = x.DATA_TYPE,
                    protectType = x.PROTECT_TYPE,
                    description = x.DESCRIPTION,
                    expressions = x.EXPRESSIONS,
                    isCollectEnabled = x.IS_COLLECTENABLED,
                    saveToDatabase = x.SAVE_TO_DATABASE,
                    showOnDashboard = x.SHOW_ON_DASHBOARD,
                    samplingIntervalMs = x.SAMPLINGINTERVALMS,
                    sortOrder = x.SORT_ORDER,
                    isEnabled = x.IsEnabled
                })
                .ToListAsync();

            return Success("조회되었습니다.", rows);
        } catch (Exception ex) {
            return ErrorData(GetErrorMessage(ex));
        }
    }

    [HttpPost, ActionName("save-major")]
    public async Task<IActionResult> SaveMajor([FromBody] JsonElement request) {
        var model = WebFlexModelMapper.PopulateDTOModel<OpcMajorGroup>(request);
        NormalizeMajorGroup(model);

        if (string.IsNullOrWhiteSpace(model.MAJOR_GROUP_NAME)) {
            return ErrorData("대그룹명을 입력해 주세요.");
        }

        await using var tran = await _db.Database.BeginTransactionAsync();

        try {
            var now = DateTime.UtcNow;

            if (string.IsNullOrWhiteSpace(model.ID)) {
                var duplicate = await _db.Set<OpcMajorGroup>()
                    .AsNoTracking()
                    .AnyAsync(x => x.MAJOR_GROUP_NAME == model.MAJOR_GROUP_NAME);

                if (duplicate) {
                    throw new WebFlexMessageException("이미 등록된 대그룹명입니다.");
                }

                model.ID = await CreateMajorGroupIdAsync();
                model.SORT_ORDER ??= await CreateMajorGroupSortOrderAsync();
                model.IsEnabled = true;
                model.CreatedAt = now;
                model.UpdatedAt = now;

                _db.Set<OpcMajorGroup>().Add(model);
            } else {
                var entity = await _db.Set<OpcMajorGroup>()
                    .FirstOrDefaultAsync(x => x.ID == model.ID);

                if (entity == null) {
                    throw new WebFlexMessageException("대그룹을 찾을 수 없습니다.");
                }

                var duplicate = await _db.Set<OpcMajorGroup>()
                    .AsNoTracking()
                    .AnyAsync(x => x.ID != model.ID && x.MAJOR_GROUP_NAME == model.MAJOR_GROUP_NAME);

                if (duplicate) {
                    throw new WebFlexMessageException("이미 등록된 대그룹명입니다.");
                }

                WebFlexModelMapper.ApplyModel(entity, request, NormalizeMajorGroup);
                entity.ID = model.ID;
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

    [HttpPost, ActionName("save-group")]
    public async Task<IActionResult> SaveGroup([FromBody] JsonElement request) {
        var model = WebFlexModelMapper.PopulateDTOModel<OpcGroup>(request);
        NormalizeGroup(model);

        if (string.IsNullOrWhiteSpace(model.GROUP_NAME)) {
            return ErrorData("중그룹명을 입력해 주세요.");
        }

        await using var tran = await _db.Database.BeginTransactionAsync();

        try {
            var now = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(model.MAJOR_GROUP_ID)) {
                var majorExists = await _db.Set<OpcMajorGroup>()
                    .AsNoTracking()
                    .AnyAsync(x => x.ID == model.MAJOR_GROUP_ID);

                if (!majorExists) {
                    throw new WebFlexMessageException("대그룹을 찾을 수 없습니다.");
                }
            }

            if (string.IsNullOrWhiteSpace(model.ID)) {
                var duplicate = await _db.Set<OpcGroup>()
                    .AsNoTracking()
                    .AnyAsync(x => x.MAJOR_GROUP_ID == model.MAJOR_GROUP_ID && x.GROUP_NAME == model.GROUP_NAME);

                if (duplicate) {
                    throw new WebFlexMessageException("이미 등록된 중그룹명입니다.");
                }

                model.ID = await CreateGroupIdAsync();
                model.SORT_ORDER ??= await CreateGroupSortOrderAsync(model.MAJOR_GROUP_ID);
                model.IsEnabled = true;
                model.CreatedAt = now;
                model.UpdatedAt = now;

                _db.Set<OpcGroup>().Add(model);
            } else {
                var entity = await _db.Set<OpcGroup>()
                    .FirstOrDefaultAsync(x => x.ID == model.ID);

                if (entity == null) {
                    throw new WebFlexMessageException("중그룹을 찾을 수 없습니다.");
                }

                var duplicate = await _db.Set<OpcGroup>()
                    .AsNoTracking()
                    .AnyAsync(x =>
                        x.ID != model.ID &&
                        x.MAJOR_GROUP_ID == model.MAJOR_GROUP_ID &&
                        x.GROUP_NAME == model.GROUP_NAME);

                if (duplicate) {
                    throw new WebFlexMessageException("이미 등록된 중그룹명입니다.");
                }

                WebFlexModelMapper.ApplyModel(entity, request, NormalizeGroup);
                entity.ID = model.ID;
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

    [HttpPost, ActionName("delete-major")]
    public async Task<IActionResult> DeleteMajor([FromBody] JsonElement request) {
        var model = WebFlexModelMapper.PopulateDTOModel<OpcMajorGroup>(request);

        if (string.IsNullOrWhiteSpace(model.ID)) {
            return ErrorData("삭제할 대그룹을 선택해 주세요.");
        }

        await using var tran = await _db.Database.BeginTransactionAsync();

        try {
            var entity = await _db.Set<OpcMajorGroup>().FirstOrDefaultAsync(x => x.ID == model.ID);
            if (entity == null) throw new WebFlexMessageException("대그룹을 찾을 수 없습니다.");

            var groups = await _db.Set<OpcGroup>().Where(x => x.MAJOR_GROUP_ID == model.ID).ToListAsync();
            foreach (var group in groups) {
                group.MAJOR_GROUP_ID = null;
                group.UpdatedAt = DateTime.UtcNow;
            }

            _db.Set<OpcMajorGroup>().Remove(entity);
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

    [HttpPost, ActionName("delete-group")]
    public async Task<IActionResult> DeleteGroup([FromBody] JsonElement request) {
        var model = WebFlexModelMapper.PopulateDTOModel<OpcGroup>(request);

        if (string.IsNullOrWhiteSpace(model.ID)) {
            return ErrorData("삭제할 중그룹을 선택해 주세요.");
        }

        await using var tran = await _db.Database.BeginTransactionAsync();

        try {
            var entity = await _db.Set<OpcGroup>().FirstOrDefaultAsync(x => x.ID == model.ID);
            if (entity == null) throw new WebFlexMessageException("중그룹을 찾을 수 없습니다.");

            var tagCount = await _db.Set<OpcTag>().CountAsync(x => x.GROUP_ID == model.ID);
            if (tagCount > 0) {
                throw new WebFlexMessageException("태그가 등록된 중그룹은 삭제할 수 없습니다. 태그를 먼저 다른 중그룹으로 이동하거나 삭제해 주세요.");
            }

            _db.Set<OpcGroup>().Remove(entity);
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

    [HttpPost, ActionName("move-tags")]
    public async Task<IActionResult> MoveTags([FromBody] JsonElement request) {
        var model = WebFlexModelMapper.PopulateDTOModel<MoveTagsModel>(request);

        if (model.TAG_IDS.Count == 0) return ErrorData("이동할 태그를 선택해 주세요.");
        if (string.IsNullOrWhiteSpace(model.GROUP_ID)) return ErrorData("이동할 중그룹을 선택해 주세요.");

        await using var tran = await _db.Database.BeginTransactionAsync();

        try {
            var groupExists = await _db.Set<OpcGroup>().AsNoTracking().AnyAsync(x => x.ID == model.GROUP_ID);
            if (!groupExists) throw new WebFlexMessageException("중그룹을 찾을 수 없습니다.");

            var tags = await _db.Set<OpcTag>().Where(x => model.TAG_IDS.Contains(x.ID)).ToListAsync();
            if (tags.Count == 0) throw new WebFlexMessageException("이동할 태그를 찾을 수 없습니다.");

            var now = DateTime.UtcNow;
            foreach (var tag in tags) {
                tag.GROUP_ID = model.GROUP_ID;
                tag.UpdatedAt = now;
            }

            await _db.SaveChangesAsync();
            await tran.CommitAsync();
            await UpdateCurrentValueGroupAsync(tags.Select(x => x.ID).ToList(), model.GROUP_ID);

            return Success($"{tags.Count}개의 태그를 이동했습니다.");
        } catch (WebFlexMessageException ex) {
            await tran.RollbackAsync();
            return ErrorData(ex.Message);
        } catch (Exception ex) {
            await tran.RollbackAsync();
            return ErrorData(GetErrorMessage(ex));
        }
    }

    [HttpPost, ActionName("delete-tags")]
    public async Task<IActionResult> DeleteTags([FromBody] JsonElement request) {
        var model = WebFlexModelMapper.PopulateDTOModel<MoveTagsModel>(request);

        if (model.TAG_IDS.Count == 0) return ErrorData("삭제할 태그를 선택해 주세요.");

        await using var tran = await _db.Database.BeginTransactionAsync();

        try {
            var tags = await _db.Set<OpcTag>().Where(x => model.TAG_IDS.Contains(x.ID)).ToListAsync();
            if (tags.Count == 0) throw new WebFlexMessageException("삭제할 태그를 찾을 수 없습니다.");

            await _tsdDb.Set<CurrentValue>().Where(x => model.TAG_IDS.Contains(x.TAG_ID)).ExecuteDeleteAsync();
            _db.Set<OpcTag>().RemoveRange(tags);

            await _db.SaveChangesAsync();
            await tran.CommitAsync();

            return Success($"{tags.Count}개의 태그가 삭제되었습니다.");
        } catch (WebFlexMessageException ex) {
            await tran.RollbackAsync();
            return ErrorData(ex.Message);
        } catch (Exception ex) {
            await tran.RollbackAsync();
            return ErrorData(GetErrorMessage(ex));
        }
    }

    private IQueryable<GroupRowModel> SelectGroupRows(IQueryable<OpcGroup> query) {
        return query
            .OrderBy(x => x.MajorGroup == null ? "ZZZZZZ" : x.MajorGroup.MAJOR_GROUP_NAME)
            .ThenBy(x => x.SORT_ORDER ?? int.MaxValue)
            .ThenBy(x => x.GROUP_NAME)
            .Select(x => new GroupRowModel {
                id = x.ID,
                majorGroupId = x.MAJOR_GROUP_ID,
                majorGroupName = x.MajorGroup == null ? null : x.MajorGroup.MAJOR_GROUP_NAME,
                name = x.GROUP_NAME,
                description = x.DESCRIPTION,
                sortOrder = x.SORT_ORDER,
                tagCount = _db.Set<OpcTag>().Count(t => t.GROUP_ID == x.ID)
            });
    }

    private async Task UpdateCurrentValueGroupAsync(List<string> tagIds, string groupId) {
        if (tagIds.Count == 0) return;

        var now = DateTime.UtcNow;
        await _tsdDb.Set<CurrentValue>()
            .Where(x => tagIds.Contains(x.TAG_ID))
            .ExecuteUpdateAsync(x => x
                .SetProperty(v => v.GROUP_ID, groupId)
                .SetProperty(v => v.UPDATEDAT, now));
    }

    private static void NormalizeMajorGroup(OpcMajorGroup model) {
        model.MAJOR_GROUP_NAME = model.MAJOR_GROUP_NAME?.Trim() ?? "";
        model.DESCRIPTION = string.IsNullOrWhiteSpace(model.DESCRIPTION) ? null : model.DESCRIPTION.Trim();
        model.SORT_ORDER = model.SORT_ORDER <= 0 ? null : model.SORT_ORDER;
    }

    private static void NormalizeGroup(OpcGroup model) {
        model.MAJOR_GROUP_ID = string.IsNullOrWhiteSpace(model.MAJOR_GROUP_ID) ? null : model.MAJOR_GROUP_ID.Trim();
        model.GROUP_NAME = model.GROUP_NAME?.Trim() ?? "";
        model.DESCRIPTION = string.IsNullOrWhiteSpace(model.DESCRIPTION) ? null : model.DESCRIPTION.Trim();
        model.SORT_ORDER = model.SORT_ORDER <= 0 ? null : model.SORT_ORDER;
    }

    private async Task<string> CreateMajorGroupIdAsync() {
        var ids = await _db.Set<OpcMajorGroup>().AsNoTracking().Select(x => x.ID).ToListAsync();
        var max = ids.Where(x => !string.IsNullOrWhiteSpace(x) && x.StartsWith("GN"))
            .Select(x => x.Replace("GN", ""))
            .Where(x => int.TryParse(x, out _))
            .Select(int.Parse)
            .DefaultIfEmpty(0)
            .Max();
        return $"GN{max + 1:0000}";
    }

    private async Task<string> CreateGroupIdAsync() {
        var ids = await _db.Set<OpcGroup>().AsNoTracking().Select(x => x.ID).ToListAsync();
        var max = ids.Where(x => !string.IsNullOrWhiteSpace(x) && x.StartsWith("DG"))
            .Select(x => x.Replace("DG", ""))
            .Where(x => int.TryParse(x, out _))
            .Select(int.Parse)
            .DefaultIfEmpty(0)
            .Max();
        return $"DG{max + 1:0000}";
    }

    private async Task<int> CreateMajorGroupSortOrderAsync() {
        var values = await _db.Set<OpcMajorGroup>().AsNoTracking().Select(x => x.SORT_ORDER).ToListAsync();
        return values.Where(x => x.HasValue).Select(x => x!.Value).DefaultIfEmpty(0).Max() + 1;
    }

    private async Task<int> CreateGroupSortOrderAsync(string? majorGroupId) {
        var values = await _db.Set<OpcGroup>().AsNoTracking().Where(x => x.MAJOR_GROUP_ID == majorGroupId).Select(x => x.SORT_ORDER).ToListAsync();
        return values.Where(x => x.HasValue).Select(x => x!.Value).DefaultIfEmpty(0).Max() + 1;
    }

    private class GroupRowModel {
        public string id { get; set; } = "";
        public string? majorGroupId { get; set; }
        public string? majorGroupName { get; set; }
        public string name { get; set; } = "";
        public string? description { get; set; }
        public int? sortOrder { get; set; }
        public int tagCount { get; set; }
    }

    private class MoveTagsModel {
        public List<string> TAG_IDS { get; set; } = new();
        public string? GROUP_ID { get; set; }
    }
}
