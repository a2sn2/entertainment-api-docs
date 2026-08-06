using FoundationKit.Application.Results;

namespace FoundationKit.Tests;

public sealed class ResultTests
{
    [Fact]
    public void Successful_result_has_no_error()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.Equal(Error.None, result.Error);
    }

    [Fact]
    public void Failed_generic_result_guards_value()
    {
        var error = Error.NotFound("Test.NotFound", "The item was not found.");
        var result = Result<string>.Failure(error);

        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);
        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void Invalid_success_state_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => InvalidResult.Create());
    }

    private sealed class InvalidResult : Result
    {
        private InvalidResult()
            : base(true, Error.Failure("Invalid", "Invalid state."))
        {
        }

        public static InvalidResult Create() => new();
    }
}
