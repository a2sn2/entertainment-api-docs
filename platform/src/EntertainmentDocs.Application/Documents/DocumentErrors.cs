using FoundationKit.Application.Results;

namespace EntertainmentDocs.Application.Documents;

public static class DocumentErrors
{
    public static readonly Error AuthenticationRequired = Error.Unauthorized(
        "Documents.AuthenticationRequired",
        "Authentication is required.");

    public static readonly Error NotFound = Error.NotFound(
        "Documents.NotFound",
        "Document was not found.");

    public static readonly Error ReferenceAlreadyExists = Error.Conflict(
        "Documents.ReferenceAlreadyExists",
        "Document reference already exists.");

    public static readonly Error SlugAlreadyExists = Error.Conflict(
        "Documents.SlugAlreadyExists",
        "Document slug already exists.");

    public static Error BusinessRule(string description) => Error.BusinessRule(
        "Documents.BusinessRuleViolation",
        description);
}
