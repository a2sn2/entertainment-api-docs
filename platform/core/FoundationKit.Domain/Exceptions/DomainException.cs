namespace FoundationKit.Domain.Exceptions;

public sealed class DomainException(string code, string message) : Exception(message)
{
    public string Code { get; } = string.IsNullOrWhiteSpace(code)
        ? throw new ArgumentException("A domain error code is required.", nameof(code))
        : code.Trim();
}
