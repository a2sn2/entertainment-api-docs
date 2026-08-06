using FoundationKit.Application.Abstractions;
using FoundationKit.Application.Persistence;
using FoundationKit.Application.Results;
using FoundationKit.Workbench.Application.Shared;
using FoundationKit.Workbench.Contracts.User;
using FoundationKit.Workbench.Domain;

namespace FoundationKit.Workbench.Application.User;

public sealed class CreateUserRequestUseCase(
    IRepository<BuildBrief, Guid> repository,
    IUnitOfWork unitOfWork,
    IClock clock,
    ICapabilityCatalog capabilityCatalog)
{
    public async Task<Result<BuildBrief>> ExecuteAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var knownCapabilities = await capabilityCatalog
            .ReadCapabilityIdsAsync(cancellationToken);
        var unknownCapabilities = request.SelectedCapabilityIds
            .Where(id => !knownCapabilities.Contains(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (unknownCapabilities.Length > 0)
        {
            return Result<BuildBrief>.Failure(Error.Validation(
                "UserRequest.UnknownCapability",
                $"Unknown capability ids: {string.Join(", ", unknownCapabilities)}"));
        }

        var result = BuildBrief.Create(
            request.ProjectName,
            request.ProjectType,
            request.Audience,
            request.Goal,
            request.SelectedCapabilityIds,
            request.Priorities,
            request.Notes,
            clock.UtcNow);

        if (result.IsFailure)
            return result;

        await repository.AddAsync(result.Value, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return result;
    }
}
