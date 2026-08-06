namespace FoundationKit.Application.Models;

public abstract record EntityDto<TId>(TId Id)
    where TId : notnull;

public abstract record AuditedEntityDto<TId>(
    TId Id,
    DateTimeOffset CreatedUtc,
    DateTimeOffset UpdatedUtc) : EntityDto<TId>(Id)
    where TId : notnull;
