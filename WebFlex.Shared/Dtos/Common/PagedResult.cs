namespace WebFlex.Shared.Dtos.Common;

public class PagedResult<T> {
    public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();

    public int TotalCount { get; set; }

    public int PageIndex { get; set; }

    public int PageSize { get; set; }

    public static PagedResult<T> Create(
        IReadOnlyList<T> items,
        int totalCount,
        int pageIndex,
        int pageSize) {
        return new PagedResult<T> {
            Items = items,
            TotalCount = totalCount,
            PageIndex = pageIndex,
            PageSize = pageSize
        };
    }
}