namespace EntertainmentDocs.Domain.Common;

public abstract class AggregateRoot : FoundationKit.Domain.Primitives.AggregateRoot<Guid>
{
    protected AggregateRoot(Guid id) : base(id) { }
    protected AggregateRoot() { }
}
