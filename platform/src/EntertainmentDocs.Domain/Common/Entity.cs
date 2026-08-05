namespace EntertainmentDocs.Domain.Common;

public abstract class Entity : FoundationKit.Domain.Primitives.Entity<Guid>
{
    protected Entity(Guid id) : base(id) { }
    protected Entity() { }
}
