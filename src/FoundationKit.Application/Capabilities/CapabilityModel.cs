namespace FoundationKit.Application.Capabilities;

public enum CapabilityKind
{
    Kernel,
    Optional,
    Provider,
    Tooling
}

public enum CapabilityMaturity
{
    Stable,
    Preview,
    ReferenceOnly,
    Planned
}

public sealed record CapabilityDescriptor(
    string Id,
    string DisplayName,
    CapabilityKind Kind,
    CapabilityMaturity Maturity,
    string Category,
    string Description,
    IReadOnlyList<string> Dependencies);

public sealed record CapabilityProfile(
    string Id,
    string DisplayName,
    string Description,
    IReadOnlyList<string> CapabilityIds);

public static class FoundationCapabilityIds
{
    public const string Kernel = "kernel";
    public const string Validation = "validation";
    public const string WebApi = "web-api";
    public const string Blazor = "blazor";
    public const string Observability = "observability";
    public const string Security = "security";
    public const string Identity = "identity";
    public const string Authorization = "authorization";
    public const string Auditing = "auditing";
    public const string Settings = "settings";
    public const string FeatureManagement = "feature-management";
    public const string Localization = "localization";
    public const string Organization = "organization";
    public const string MultiTenancy = "multi-tenancy";
    public const string Workflow = "workflow";
    public const string Approvals = "approvals";
    public const string Tasks = "tasks";
    public const string Notifications = "notifications";
    public const string Files = "files";
    public const string Documents = "documents";
    public const string Jobs = "jobs";
    public const string Messaging = "messaging";
    public const string Webhooks = "webhooks";
    public const string Realtime = "realtime";
    public const string Caching = "caching";
    public const string Search = "search";
    public const string Reporting = "reporting";
    public const string Idempotency = "idempotency";
    public const string Concurrency = "concurrency";
    public const string Money = "money";
    public const string Numbering = "numbering";
    public const string Privacy = "privacy";
    public const string Retention = "retention";
    public const string ArtificialIntelligence = "ai";
    public const string SqlServerProvider = "provider-sqlserver";
    public const string RedisProvider = "provider-redis";
    public const string SmtpProvider = "provider-smtp";
    public const string CliTooling = "tooling-cli";
    public const string WorkbenchTooling = "tooling-workbench";
}

public static class FoundationCapabilityCatalog
{
    private static readonly IReadOnlyList<string> NoDependencies = Array.Empty<string>();

    private static readonly CapabilityDescriptor[] Descriptors =
    [
        new(FoundationCapabilityIds.Kernel, "Kernel", CapabilityKind.Kernel, CapabilityMaturity.Stable, "Foundation", "Domain/application primitives that every FoundationKit composition starts from.", NoDependencies),
        new(FoundationCapabilityIds.Validation, "Validation", CapabilityKind.Optional, CapabilityMaturity.Stable, "Foundation", "Reusable validation and business-rule boundaries.", [FoundationCapabilityIds.Kernel]),
        new(FoundationCapabilityIds.WebApi, "Web API", CapabilityKind.Optional, CapabilityMaturity.Stable, "Experience", "HTTP result mapping, correlation and reusable API conventions.", [FoundationCapabilityIds.Kernel]),
        new(FoundationCapabilityIds.Blazor, "Blazor", CapabilityKind.Optional, CapabilityMaturity.Stable, "Experience", "Reusable API client, state and MVVM building blocks for Blazor consumers.", [FoundationCapabilityIds.Kernel]),
        new(FoundationCapabilityIds.Observability, "Observability", CapabilityKind.Optional, CapabilityMaturity.Preview, "Operations", "Logs, traces, metrics, correlation and health abstractions.", [FoundationCapabilityIds.Kernel]),
        new(FoundationCapabilityIds.Security, "Security", CapabilityKind.Optional, CapabilityMaturity.Preview, "Security", "Secure defaults and hooks for rate limiting, CSRF, step-up authentication and security events.", [FoundationCapabilityIds.WebApi]),
        new(FoundationCapabilityIds.Identity, "Identity", CapabilityKind.Optional, CapabilityMaturity.ReferenceOnly, "Identity", "Authentication lifecycle, sessions, MFA, confirmation and recovery contracts.", [FoundationCapabilityIds.Security]),
        new(FoundationCapabilityIds.Authorization, "Authorization", CapabilityKind.Optional, CapabilityMaturity.ReferenceOnly, "Identity", "Role, permission, policy, ownership and scoped authorization model.", [FoundationCapabilityIds.Identity]),
        new(FoundationCapabilityIds.Auditing, "Auditing", CapabilityKind.Optional, CapabilityMaturity.ReferenceOnly, "Governance", "Business and security audit trails with actor, target and correlation context.", [FoundationCapabilityIds.Kernel]),
        new(FoundationCapabilityIds.Settings, "Settings", CapabilityKind.Optional, CapabilityMaturity.Planned, "Platform", "Global, tenant, organization and user-scoped settings.", [FoundationCapabilityIds.Kernel]),
        new(FoundationCapabilityIds.FeatureManagement, "Feature Management", CapabilityKind.Optional, CapabilityMaturity.Planned, "Platform", "Feature flags and staged enablement by context.", [FoundationCapabilityIds.Settings]),
        new(FoundationCapabilityIds.Localization, "Localization", CapabilityKind.Optional, CapabilityMaturity.Planned, "Experience", "Language, culture, RTL/LTR, time-zone and formatting conventions.", [FoundationCapabilityIds.Kernel]),
        new(FoundationCapabilityIds.Organization, "Organization", CapabilityKind.Optional, CapabilityMaturity.Planned, "Business", "Organizations, branches, departments, teams, positions and reporting hierarchy.", [FoundationCapabilityIds.Kernel]),
        new(FoundationCapabilityIds.MultiTenancy, "Multi-Tenancy", CapabilityKind.Optional, CapabilityMaturity.Planned, "Platform", "Tenant context and isolation patterns without forcing a storage topology.", [FoundationCapabilityIds.Authorization]),
        new(FoundationCapabilityIds.Workflow, "Workflow", CapabilityKind.Optional, CapabilityMaturity.Planned, "Process", "Stateful business workflows, transitions, escalation and history.", [FoundationCapabilityIds.Auditing]),
        new(FoundationCapabilityIds.Approvals, "Approvals", CapabilityKind.Optional, CapabilityMaturity.Planned, "Process", "Single, sequential, parallel, quorum and maker-checker approvals.", [FoundationCapabilityIds.Workflow, FoundationCapabilityIds.Authorization, FoundationCapabilityIds.Auditing]),
        new(FoundationCapabilityIds.Tasks, "Tasks", CapabilityKind.Optional, CapabilityMaturity.Planned, "Process", "Assignable work items, priorities, due dates and lifecycle tracking.", [FoundationCapabilityIds.Kernel]),
        new(FoundationCapabilityIds.Notifications, "Notifications", CapabilityKind.Optional, CapabilityMaturity.Planned, "Communication", "Channel-neutral notification contracts for in-app, email, SMS, push and other adapters.", [FoundationCapabilityIds.Kernel]),
        new(FoundationCapabilityIds.Files, "Files", CapabilityKind.Optional, CapabilityMaturity.Planned, "Content", "Provider-neutral file storage, metadata, integrity and access contracts.", [FoundationCapabilityIds.Authorization]),
        new(FoundationCapabilityIds.Documents, "Documents", CapabilityKind.Optional, CapabilityMaturity.Planned, "Content", "Document metadata, classification, versioning and entity linkage.", [FoundationCapabilityIds.Files, FoundationCapabilityIds.Auditing]),
        new(FoundationCapabilityIds.Jobs, "Background Jobs", CapabilityKind.Optional, CapabilityMaturity.Planned, "Operations", "Immediate, delayed, scheduled and recurring background work contracts.", [FoundationCapabilityIds.Kernel]),
        new(FoundationCapabilityIds.Messaging, "Messaging", CapabilityKind.Optional, CapabilityMaturity.Planned, "Integration", "Integration events, outbox/inbox boundaries, retries and dead-letter concepts.", [FoundationCapabilityIds.Kernel]),
        new(FoundationCapabilityIds.Webhooks, "Webhooks", CapabilityKind.Optional, CapabilityMaturity.Planned, "Integration", "Inbound/outbound webhooks, signatures, replay protection and delivery history.", [FoundationCapabilityIds.Messaging, FoundationCapabilityIds.Security]),
        new(FoundationCapabilityIds.Realtime, "Realtime", CapabilityKind.Optional, CapabilityMaturity.Planned, "Communication", "Realtime event delivery abstractions for SignalR/WebSocket-style providers.", [FoundationCapabilityIds.Authorization]),
        new(FoundationCapabilityIds.Caching, "Caching", CapabilityKind.Optional, CapabilityMaturity.Planned, "Data", "Memory/distributed cache abstraction with TTL and invalidation semantics.", [FoundationCapabilityIds.Kernel]),
        new(FoundationCapabilityIds.Search, "Search", CapabilityKind.Optional, CapabilityMaturity.Planned, "Data", "Provider-neutral search contracts for relational, full-text and external search engines.", [FoundationCapabilityIds.Authorization]),
        new(FoundationCapabilityIds.Reporting, "Reporting", CapabilityKind.Optional, CapabilityMaturity.Planned, "Business", "Report definitions, filtering, grouping and export boundaries.", [FoundationCapabilityIds.Authorization]),
        new(FoundationCapabilityIds.Idempotency, "Idempotency", CapabilityKind.Optional, CapabilityMaturity.ReferenceOnly, "Reliability", "Duplicate-write prevention for retried HTTP and integration operations.", [FoundationCapabilityIds.WebApi]),
        new(FoundationCapabilityIds.Concurrency, "Concurrency", CapabilityKind.Optional, CapabilityMaturity.ReferenceOnly, "Reliability", "Optimistic concurrency and conflict-detection conventions.", [FoundationCapabilityIds.Kernel]),
        new(FoundationCapabilityIds.Money, "Money", CapabilityKind.Optional, CapabilityMaturity.Planned, "Finance", "Currency-aware money values and explicit conversion boundaries.", [FoundationCapabilityIds.Kernel]),
        new(FoundationCapabilityIds.Numbering, "Numbering", CapabilityKind.Optional, CapabilityMaturity.Planned, "Business", "Business-friendly sequences with prefixes, periods and organizational scope.", [FoundationCapabilityIds.Kernel]),
        new(FoundationCapabilityIds.Privacy, "Privacy", CapabilityKind.Optional, CapabilityMaturity.Planned, "Governance", "PII classification, masking, redaction, consent and anonymization hooks.", [FoundationCapabilityIds.Auditing, FoundationCapabilityIds.Security]),
        new(FoundationCapabilityIds.Retention, "Retention", CapabilityKind.Optional, CapabilityMaturity.Planned, "Governance", "Retention, archive, deletion and anonymization scheduling contracts.", [FoundationCapabilityIds.Jobs, FoundationCapabilityIds.Auditing]),
        new(FoundationCapabilityIds.ArtificialIntelligence, "AI", CapabilityKind.Optional, CapabilityMaturity.Planned, "Intelligence", "Provider-neutral chat, embeddings, retrieval and agent abstractions.", [FoundationCapabilityIds.Observability]),
        new(FoundationCapabilityIds.SqlServerProvider, "SQL Server Provider", CapabilityKind.Provider, CapabilityMaturity.ReferenceOnly, "Provider", "SQL Server adapter family owned outside the provider-agnostic kernel.", [FoundationCapabilityIds.Kernel]),
        new(FoundationCapabilityIds.RedisProvider, "Redis Provider", CapabilityKind.Provider, CapabilityMaturity.Planned, "Provider", "Redis adapter for caching and related distributed primitives.", [FoundationCapabilityIds.Caching]),
        new(FoundationCapabilityIds.SmtpProvider, "SMTP Provider", CapabilityKind.Provider, CapabilityMaturity.ReferenceOnly, "Provider", "SMTP delivery adapter for notification and account-security messages.", [FoundationCapabilityIds.Notifications]),
        new(FoundationCapabilityIds.CliTooling, "FoundationKit CLI", CapabilityKind.Tooling, CapabilityMaturity.Planned, "Tooling", "Interactive project composer driven by the capability graph and project manifest.", [FoundationCapabilityIds.Kernel]),
        new(FoundationCapabilityIds.WorkbenchTooling, "FoundationKit Workbench", CapabilityKind.Tooling, CapabilityMaturity.ReferenceOnly, "Tooling", "Interactive repository consumer and future visual project composer.", [FoundationCapabilityIds.Kernel])
    ];

    public static IReadOnlyList<CapabilityDescriptor> All => Descriptors;
}

public static class FoundationCapabilityProfiles
{
    public const string Minimal = "minimal";
    public const string Standard = "standard";
    public const string Enterprise = "enterprise";
    public const string Fintech = "fintech";
    public const string SaaS = "saas";
    public const string InternalBusiness = "internal-business";
    public const string PublicPortal = "public-portal";

    private static readonly CapabilityProfile[] Profiles =
    [
        new(Minimal, "Minimal", "Small API/service foundation with validation and operational visibility.",
            [FoundationCapabilityIds.Kernel, FoundationCapabilityIds.Validation, FoundationCapabilityIds.WebApi, FoundationCapabilityIds.Observability]),
        new(Standard, "Standard", "General business-system baseline with identity, security, audit and common user-facing capabilities.",
            [FoundationCapabilityIds.Kernel, FoundationCapabilityIds.Validation, FoundationCapabilityIds.WebApi, FoundationCapabilityIds.Observability, FoundationCapabilityIds.Security, FoundationCapabilityIds.Identity, FoundationCapabilityIds.Authorization, FoundationCapabilityIds.Auditing, FoundationCapabilityIds.Settings, FoundationCapabilityIds.Notifications, FoundationCapabilityIds.Files, FoundationCapabilityIds.Localization]),
        new(Enterprise, "Enterprise", "Standard baseline plus organizational process, approvals, automation, messaging and reporting.",
            [FoundationCapabilityIds.Kernel, FoundationCapabilityIds.Validation, FoundationCapabilityIds.WebApi, FoundationCapabilityIds.Observability, FoundationCapabilityIds.Security, FoundationCapabilityIds.Identity, FoundationCapabilityIds.Authorization, FoundationCapabilityIds.Auditing, FoundationCapabilityIds.Settings, FoundationCapabilityIds.Notifications, FoundationCapabilityIds.Files, FoundationCapabilityIds.Localization, FoundationCapabilityIds.Organization, FoundationCapabilityIds.Workflow, FoundationCapabilityIds.Approvals, FoundationCapabilityIds.Tasks, FoundationCapabilityIds.Jobs, FoundationCapabilityIds.Messaging, FoundationCapabilityIds.FeatureManagement, FoundationCapabilityIds.Reporting, FoundationCapabilityIds.Idempotency, FoundationCapabilityIds.Concurrency]),
        new(Fintech, "Fintech", "Enterprise baseline plus money, privacy, numbering and retention-oriented controls.",
            [FoundationCapabilityIds.Kernel, FoundationCapabilityIds.Validation, FoundationCapabilityIds.WebApi, FoundationCapabilityIds.Observability, FoundationCapabilityIds.Security, FoundationCapabilityIds.Identity, FoundationCapabilityIds.Authorization, FoundationCapabilityIds.Auditing, FoundationCapabilityIds.Settings, FoundationCapabilityIds.Notifications, FoundationCapabilityIds.Files, FoundationCapabilityIds.Localization, FoundationCapabilityIds.Organization, FoundationCapabilityIds.Workflow, FoundationCapabilityIds.Approvals, FoundationCapabilityIds.Tasks, FoundationCapabilityIds.Jobs, FoundationCapabilityIds.Messaging, FoundationCapabilityIds.FeatureManagement, FoundationCapabilityIds.Reporting, FoundationCapabilityIds.Idempotency, FoundationCapabilityIds.Concurrency, FoundationCapabilityIds.Money, FoundationCapabilityIds.Privacy, FoundationCapabilityIds.Numbering, FoundationCapabilityIds.Retention]),
        new(SaaS, "SaaS", "Standard baseline with tenant isolation, feature flags, async work, integrations, caching and search.",
            [FoundationCapabilityIds.Kernel, FoundationCapabilityIds.Validation, FoundationCapabilityIds.WebApi, FoundationCapabilityIds.Observability, FoundationCapabilityIds.Security, FoundationCapabilityIds.Identity, FoundationCapabilityIds.Authorization, FoundationCapabilityIds.Auditing, FoundationCapabilityIds.Settings, FoundationCapabilityIds.Notifications, FoundationCapabilityIds.Files, FoundationCapabilityIds.Localization, FoundationCapabilityIds.MultiTenancy, FoundationCapabilityIds.FeatureManagement, FoundationCapabilityIds.Jobs, FoundationCapabilityIds.Webhooks, FoundationCapabilityIds.Caching, FoundationCapabilityIds.Search]),
        new(InternalBusiness, "Internal Business", "Internal line-of-business systems with organization, workflow, approvals, tasks, reporting and numbering.",
            [FoundationCapabilityIds.Kernel, FoundationCapabilityIds.Validation, FoundationCapabilityIds.WebApi, FoundationCapabilityIds.Observability, FoundationCapabilityIds.Security, FoundationCapabilityIds.Identity, FoundationCapabilityIds.Authorization, FoundationCapabilityIds.Auditing, FoundationCapabilityIds.Settings, FoundationCapabilityIds.Notifications, FoundationCapabilityIds.Files, FoundationCapabilityIds.Localization, FoundationCapabilityIds.Organization, FoundationCapabilityIds.Workflow, FoundationCapabilityIds.Approvals, FoundationCapabilityIds.Tasks, FoundationCapabilityIds.Reporting, FoundationCapabilityIds.Numbering]),
        new(PublicPortal, "Public Portal", "Externally facing portal baseline with identity, files, notifications, search and localization.",
            [FoundationCapabilityIds.Kernel, FoundationCapabilityIds.Validation, FoundationCapabilityIds.WebApi, FoundationCapabilityIds.Observability, FoundationCapabilityIds.Security, FoundationCapabilityIds.Identity, FoundationCapabilityIds.Authorization, FoundationCapabilityIds.Files, FoundationCapabilityIds.Notifications, FoundationCapabilityIds.Search, FoundationCapabilityIds.Localization])
    ];

    public static IReadOnlyList<CapabilityProfile> All => Profiles;

    public static CapabilityProfile Get(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        return Profiles.FirstOrDefault(profile => string.Equals(profile.Id, id, StringComparison.OrdinalIgnoreCase))
            ?? throw new KeyNotFoundException($"Unknown FoundationKit capability profile '{id}'.");
    }
}

public sealed class CapabilityResolver
{
    private readonly Dictionary<string, CapabilityDescriptor> _descriptors;

    public CapabilityResolver(IEnumerable<CapabilityDescriptor> descriptors)
    {
        ArgumentNullException.ThrowIfNull(descriptors);

        _descriptors = descriptors.ToDictionary(descriptor => descriptor.Id, StringComparer.OrdinalIgnoreCase);
    }

    public static CapabilityResolver CreateDefault() => new(FoundationCapabilityCatalog.All);

    public IReadOnlyList<CapabilityDescriptor> Resolve(IEnumerable<string> requestedCapabilityIds)
    {
        ArgumentNullException.ThrowIfNull(requestedCapabilityIds);

        var requested = requestedCapabilityIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var visitState = new Dictionary<string, VisitState>(StringComparer.OrdinalIgnoreCase);
        var resolved = new List<CapabilityDescriptor>();

        foreach (var id in requested)
        {
            Visit(id, visitState, resolved);
        }

        return resolved;
    }

    public IReadOnlyList<CapabilityDescriptor> ResolveSelection(
        string profileId,
        IEnumerable<string>? include = null,
        IEnumerable<string>? exclude = null)
    {
        var profile = FoundationCapabilityProfiles.Get(profileId);
        var excluded = new HashSet<string>(exclude ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
        var requested = new HashSet<string>(profile.CapabilityIds, StringComparer.OrdinalIgnoreCase);

        requested.ExceptWith(excluded);
        requested.UnionWith(include ?? Array.Empty<string>());

        var resolved = Resolve(requested);
        var excludedDependency = resolved.FirstOrDefault(descriptor => excluded.Contains(descriptor.Id));

        if (excludedDependency is not null)
        {
            throw new InvalidOperationException(
                $"Capability '{excludedDependency.Id}' cannot be excluded because another selected capability requires it.");
        }

        return resolved;
    }

    private void Visit(
        string id,
        IDictionary<string, VisitState> visitState,
        ICollection<CapabilityDescriptor> resolved)
    {
        if (!_descriptors.TryGetValue(id, out var descriptor))
        {
            throw new KeyNotFoundException($"Unknown FoundationKit capability '{id}'.");
        }

        if (visitState.TryGetValue(id, out var state))
        {
            if (state == VisitState.Visited)
            {
                return;
            }

            throw new InvalidOperationException($"Capability dependency cycle detected at '{id}'.");
        }

        visitState[id] = VisitState.Visiting;

        foreach (var dependency in descriptor.Dependencies)
        {
            Visit(dependency, visitState, resolved);
        }

        visitState[id] = VisitState.Visited;
        resolved.Add(descriptor);
    }

    private enum VisitState
    {
        Visiting,
        Visited
    }
}

public sealed record FoundationKitProjectManifest(
    string Name,
    string Profile,
    IReadOnlyList<string> IncludeCapabilities,
    IReadOnlyList<string> ExcludeCapabilities,
    IReadOnlyList<string> Providers)
{
    public IReadOnlyList<CapabilityDescriptor> Resolve(CapabilityResolver resolver)
    {
        ArgumentNullException.ThrowIfNull(resolver);

        var selected = resolver.ResolveSelection(Profile, IncludeCapabilities, ExcludeCapabilities);
        var allRequested = selected.Select(descriptor => descriptor.Id).Concat(Providers);
        return resolver.Resolve(allRequested);
    }
}
