using FoundationKit.Domain.Events;

namespace FoundationKit.Workbench.Domain;

public sealed record BuildBriefReviewed(
    Guid BuildBriefId,
    BuildBriefStatus Status,
    DateTimeOffset ReviewedUtc) : IDomainEvent;
