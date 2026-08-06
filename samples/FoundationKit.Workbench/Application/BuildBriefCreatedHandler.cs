using FoundationKit.Application.Events;
using FoundationKit.Workbench.Domain;

namespace FoundationKit.Workbench.Application;

public sealed class BuildBriefCreatedHandler(
    ILogger<BuildBriefCreatedHandler> logger) : IDomainEventHandler<BuildBriefCreated>
{
    public Task HandleAsync(
        BuildBriefCreated domainEvent,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Build brief {BuildBriefId} for {ProjectName} was saved at {CreatedUtc}.",
            domainEvent.BuildBriefId,
            domainEvent.ProjectName,
            domainEvent.CreatedUtc);

        return Task.CompletedTask;
    }
}
