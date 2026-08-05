using FoundationKit.Domain.Primitives;

namespace FoundationKit.Tests;

public sealed class EntityTests
{
    [Fact]
    public void Entities_with_same_non_default_identifier_are_equal()
    {
        var id = Guid.Parse("11111111-1111-1111-1111-111111111111");

        Assert.Equal(new TestEntity(id), new TestEntity(id));
    }

    [Fact]
    public void Different_transient_entities_are_not_equal()
    {
        var first = new TestEntity();
        var second = new TestEntity();

        Assert.NotEqual(first, second);
        Assert.Equal(first, first);
    }

    private sealed class TestEntity : Entity<Guid>
    {
        public TestEntity(Guid id) : base(id) { }
        public TestEntity() { }
    }
}
