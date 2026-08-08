namespace Madar.Application.Cases;

public interface ICaseSlaPolicy
{
    TimeSpan? ResolveDuration(string? priority);
}
