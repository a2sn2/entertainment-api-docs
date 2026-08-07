using FoundationKit.Application.Events;
using FoundationKit.Workbench.Domain;
using Microsoft.Extensions.Logging;

namespace FoundationKit.Workbench.Application;

public sealed class BuildBriefCreatedHandler(
    ILogger<BuildBriefCreatedHandler> logger) : IDomainEventHandler<BuildBriefCreated>
{
    private static readonly Action<ILogger, Guid, string, DateTimeOffset, Exception?> BuildBriefCreatedLog =
        LoggerMessage.Define<Guid, string, DateTimeOffset>(
            LogLevel.Information,
            new EventId(1001, nameof(BuildBriefCreated)),
            "Build brief {BuildBriefId} for {ProjectName} was saved at {CreatedUtc}.");

    public Task HandleAsync(
        BuildBriefCreated domainEvent,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        BuildBriefCreatedLog(
            logger,
            domainEvent.BuildBriefId,
            domainEvent.ProjectName,
            domainEvent.CreatedUtc,
            null);

        return Task.CompletedTask;
    }
}
