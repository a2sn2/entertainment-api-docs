namespace FoundationKit.Application.Pagination;

public sealed record PageRequest
{
    public const int DefaultPageSize = 20;
    public const int MaximumPageSize = 200;

    public PageRequest(int page = 1, int pageSize = DefaultPageSize)
    {
        Page = Math.Max(1, page);
        PageSize = Math.Clamp(pageSize, 1, MaximumPageSize);
    }

    public int Page { get; }

    public int PageSize { get; }

    public int Skip => (Page - 1) * PageSize;
}
