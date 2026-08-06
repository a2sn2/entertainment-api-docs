using System.ComponentModel.DataAnnotations;
using FoundationKit.Application.Models;
using FoundationKit.Application.Pagination;

namespace Athar.Contracts;

public static class AtharRoles
{
    public const string User = "User";
    public const string Administrator = "Administrator";
}

public static class InitiativeWorkflow
{
    public const string Submitted = "submitted";
    public const string Approved = "approved";
    public const string Rejected = "rejected";
    public const string Approve = "approve";
    public const string Reject = "reject";
}

public static class AtharRoutes
{
    public const string ApiRoot = "api/v1";
    public const string SecurityToken = $"{ApiRoot}/security/antiforgery";
    public const string Register = $"{ApiRoot}/auth/register";
    public const string Login = $"{ApiRoot}/auth/login";
    public const string Logout = $"{ApiRoot}/auth/logout";
    public const string Me = $"{ApiRoot}/auth/me";
    public const string Initiatives = $"{ApiRoot}/initiatives";
    public const string MyInitiatives = $"{ApiRoot}/initiatives/mine";
    public const string AdminQueue = $"{ApiRoot}/admin/initiatives";
    public const string AdminDashboard = $"{ApiRoot}/admin/dashboard";

    public static string Initiative(Guid id) => $"{Initiatives}/{id:D}";

    public static string ReviewInitiative(Guid id) =>
        $"{ApiRoot}/admin/initiatives/{id:D}/review";
}

public sealed record AntiforgeryTokenResponse(string RequestToken);

public sealed class RegisterRequest
{
    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(120, MinimumLength = 2)]
    public string DisplayName { get; set; } = string.Empty;

    [Required, StringLength(128, MinimumLength = 10)]
    public string Password { get; set; } = string.Empty;
}

public sealed class LoginRequest
{
    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(128, MinimumLength = 10)]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
}

public sealed record CurrentUserResponse(
    Guid? Id,
    string? Email,
    string? DisplayName,
    IReadOnlyList<string> Roles,
    bool IsAuthenticated);

public sealed class CreateInitiativeRequest
{
    public Guid ClientRequestId { get; set; } = Guid.NewGuid();

    [Required, StringLength(140, MinimumLength = 4)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(1800, MinimumLength = 30)]
    public string Summary { get; set; } = string.Empty;

    [Required, StringLength(80, MinimumLength = 2)]
    public string Category { get; set; } = string.Empty;

    [Required, StringLength(80, MinimumLength = 2)]
    public string City { get; set; } = string.Empty;

    [Range(0, 100_000_000)]
    public decimal RequestedBudget { get; set; }

    [Range(1, 10_000_000)]
    public int TargetBeneficiaries { get; set; }
}

public sealed record InitiativeSummaryDto(
    Guid Id,
    DateTimeOffset CreatedUtc,
    DateTimeOffset UpdatedUtc,
    string Title,
    string Category,
    string City,
    decimal RequestedBudget,
    int TargetBeneficiaries,
    string Status) : AuditedEntityDto<Guid>(Id, CreatedUtc, UpdatedUtc);

public sealed record InitiativeDetailsDto(
    Guid Id,
    DateTimeOffset CreatedUtc,
    DateTimeOffset UpdatedUtc,
    string Title,
    string Summary,
    string Category,
    string City,
    decimal RequestedBudget,
    int TargetBeneficiaries,
    string Status,
    string OwnerDisplayName,
    IReadOnlyList<InitiativeReviewDto> Reviews)
    : AuditedEntityDto<Guid>(Id, CreatedUtc, UpdatedUtc);

public sealed record InitiativeReviewDto(
    Guid Id,
    Guid InitiativeId,
    string Decision,
    string ReviewerDisplayName,
    string Notes,
    DateTimeOffset ReviewedUtc) : EntityDto<Guid>(Id);

public sealed class InitiativeSearchRequest
{
    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = PageRequest.DefaultPageSize;

    [StringLength(120)]
    public string? Search { get; set; }

    [StringLength(30)]
    public string? Status { get; set; }
}

public sealed class ReviewInitiativeRequest
{
    [Required, RegularExpression("approve|reject")]
    public string Decision { get; set; } = string.Empty;

    [StringLength(1200)]
    public string? Notes { get; set; }
}

public sealed record AdminDashboardResponse(
    int Submitted,
    int Approved,
    int Rejected,
    int Total,
    decimal ApprovedBudget,
    int ApprovedBeneficiaries);

public sealed record ApiMessageResponse(string Message);
