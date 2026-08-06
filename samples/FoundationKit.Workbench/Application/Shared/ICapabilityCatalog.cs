namespace FoundationKit.Workbench.Application.Shared;

public interface ICapabilityCatalog
{
    Task<IReadOnlySet<string>> ReadCapabilityIdsAsync(
        CancellationToken cancellationToken = default);
}
