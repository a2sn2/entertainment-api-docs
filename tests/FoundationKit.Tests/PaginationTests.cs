using FoundationKit.Application.Pagination;

namespace FoundationKit.Tests;

public sealed class PaginationTests
{
    [Theory]
    [InlineData(0, 0, 1, 1)]
    [InlineData(2, 50, 2, 50)]
    [InlineData(1, 500, 1, PageRequest.MaximumPageSize)]
    public void Page_request_normalizes_bounds(
        int pageNumber,
        int pageSize,
        int expectedPage,
        int expectedPageSize)
    {
        var request = new PageRequest(pageNumber, pageSize);

        Assert.Equal(expectedPage, request.Page);
        Assert.Equal(expectedPageSize, request.PageSize);
    }

    [Fact]
    public void Paged_result_calculates_navigation()
    {
        var result = new PagedResult<int>([1, 2], 2, 2, 5);

        Assert.Equal(3, result.TotalPages);
        Assert.True(result.HasPreviousPage);
        Assert.True(result.HasNextPage);
    }
}
