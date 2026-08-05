namespace FoundationKit.Domain.Primitives;

public abstract class Entity<TId> where TId : notnull
{
    protected Entity(TId id) => Id = id;
    protected Entity() { Id = default!; }

    public TId Id { get; protected set; }

    public override bool Equals(object? obj)
    {
        if (obj is null || obj.GetType() != GetType()) return false;
        if (ReferenceEquals(this, obj)) return true;
        return EqualityComparer<TId>.Default.Equals(Id, ((Entity<TId>)obj).Id);
    }

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right) => Equals(left, right);
    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) => !Equals(left, right);
}
