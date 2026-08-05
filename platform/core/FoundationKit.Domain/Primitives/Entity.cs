using System.Runtime.CompilerServices;

namespace FoundationKit.Domain.Primitives;

public abstract class Entity<TId> where TId : notnull
{
    protected Entity(TId id) => Id = id;
    protected Entity() { Id = default!; }

    public TId Id { get; protected set; }

    private bool IsTransient => EqualityComparer<TId>.Default.Equals(Id, default!);

    public override bool Equals(object? obj)
    {
        if (obj is null || obj.GetType() != GetType()) return false;
        if (ReferenceEquals(this, obj)) return true;

        var other = (Entity<TId>)obj;
        if (IsTransient || other.IsTransient) return false;
        return EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    public override int GetHashCode() => IsTransient
        ? RuntimeHelpers.GetHashCode(this)
        : HashCode.Combine(GetType(), Id);

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right) => Equals(left, right);
    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) => !Equals(left, right);
}
