namespace WebFlex.Shared.Dtos.Common;

public class PagedRequest {
    public int PageIndex { get; set; } = 0;

    public int PageSize { get; set; } = 20;

    public string? Keyword { get; set; }

    public string? SortColumn { get; set; }

    public bool SortDesc { get; set; }

    public int Skip => PageIndex <= 0 ? 0 : PageIndex * PageSize;
}