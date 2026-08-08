using FoundationKit.Authorization;
using Madar.Application.Security;
using Madar.Domain.Cases;

namespace Madar.Application.Cases;

internal static class CaseAccessRules
{
    internal static bool CanRead(
        Case item,
        Guid userId,
        IAuthorizationEvaluator authorization) =>
        item.CreatedByUserId == userId
        || item.AssignedToUserId == userId
        || authorization.HasPermission(MadarPermissions.ReadAllCases);
}
