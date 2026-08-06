using FoundationKit.Domain.Primitives;

namespace FoundationKit.Tests;

public sealed class ValueObjectTests
{
    [Fact]
    public void Equal_components_produce_equal_value_objects()
    {
        var left = new Money(10m, "USD");
        var right = new Money(10m, "USD");

        Assert.Equal(left, right);
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
    }

    [Fact]
    public void Different_components_produce_different_value_objects()
    {
        Assert.NotEqual(new Money(10m, "USD"), new Money(11m, "USD"));
    }

    private sealed class Money(decimal amount, string currency) : ValueObject
    {
        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return amount;
            yield return currency;
        }
    }
}
