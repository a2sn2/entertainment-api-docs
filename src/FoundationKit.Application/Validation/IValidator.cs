namespace FoundationKit.Application.Validation;

public interface IValidator<in T>
{
    ValueTask<IReadOnlyList<ValidationFailure>> ValidateAsync(
        T instance,
        CancellationToken cancellationToken = default);
}

public sealed record ValidationFailure(
    string PropertyName,
    string ErrorCode,
    string Message);
