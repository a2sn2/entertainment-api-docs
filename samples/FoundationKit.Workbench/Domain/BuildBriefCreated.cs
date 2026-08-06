using FoundationKit.Domain.Events;

namespace FoundationKit.Workbench.Domain;

public sealed record BuildBriefCreated(
    Guid BuildBriefId,
    string ProjectName,
    DateTimeOffset CreatedUtc) : IDomainEvent;
