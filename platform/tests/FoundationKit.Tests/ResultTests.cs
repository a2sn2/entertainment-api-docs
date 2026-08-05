using FoundationKit.Application.Results;

namespace FoundationKit.Tests;

public sealed class ResultTests
{
    [Fact]
    public void Success_contains_value_and_no_error()
    {
        var result = Result<Guid>.Success(Guid.Parse("11111111-1111-1111-1111-111111111111"));

        Assert.True(result.IsSuccess);
        Assert.Equal(Error.None, result.Error);
        Assert.Equal(Guid.Parse("11111111-1111-1111-1111-111111111111"), result.Value);
    }

    [Fact]
    public void Failure_contains_typed_error_and_rejects_value_access()
    {
        var error = Error.NotFound("Tests.NotFound", "The item was not found.");
        var result = Result<Guid>.Failure(error);

        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);
        Assert.Throws<InvalidOperationException>(() => { _ = result.Value; });
    }
}
