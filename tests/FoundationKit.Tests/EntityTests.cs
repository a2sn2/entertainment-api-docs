using FoundationKit.Domain.Primitives;

namespace FoundationKit.Tests;

public sealed class EntityTests
{
    [Fact]
    public void Entities_with_same_non_default_identifier_are_equal()
    {
        var id = Guid.NewGuid();

        Assert.Equal(new TestEntity(id), new TestEntity(id));
    }

    [Fact]
    public void Different_transient_entities_are_not_equal()
    {
        var left = new TestEntity();
        var right = new TestEntity();

        Assert.NotEqual(left, right);
        Assert.Equal(left, left);
    }

    [Fact]
    public void Different_entity_types_with_same_identifier_are_not_equal()
    {
        var id = Guid.NewGuid();

        Assert.False(new TestEntity(id).Equals(new OtherEntity(id)));
    }

    private sealed class TestEntity : Entity<Guid>
    {
        public TestEntity(Guid id)
            : base(id)
        {
        }

        public TestEntity()
        {
        }
    }

    private sealed class OtherEntity : Entity<Guid>
    {
        public OtherEntity(Guid id)
            : base(id)
        {
        }
    }
}
