using Cephalon.Abstractions.AppModel;
using Cephalon.Abstractions.AppModel.Scaffolding;
using Cephalon.Abstractions.Patterns;
using Cephalon.Abstractions.Technologies;
using Cephalon.Abstractions.Transports;
using Cephalon.Engine.Patterns;
using Cephalon.Engine.Technologies;

namespace Cephalon.Engine.AppModel;

internal sealed class AppProfileBuilder
{
    private readonly Dictionary<string, PatternDescriptor> patterns =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, TransportDescriptor> transports =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, TechnologyDescriptor> technologyCatalog =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> selectedTechnologies =
        new(StringComparer.OrdinalIgnoreCase);
    private DataSelection dataSelection = DataSelection.Empty;
    private DatabaseTopologySelection databaseSelection = DatabaseTopologySelection.Empty;
    private IdentitySelection identitySelection = IdentitySelection.Empty;
    private TenancySelection tenancySelection = TenancySelection.Empty;
    private AuditSelection auditSelection = AuditSelection.Empty;
    private MessagingSelection messagingSelection = MessagingSelection.Empty;
    private ResilienceSelection resilienceSelection = ResilienceSelection.Empty;

    private AppBlueprint blueprint = BuiltInBlueprints.ModularMonolith;

    public AppProfileBuilder()
    {
        RegisterTechnologies(BuiltInTechnologies.All);
    }

    public void UseBlueprint(AppBlueprint value)
    {
        blueprint = value ?? throw new ArgumentNullException(nameof(value));
    }

    public void AddPattern(PatternDescriptor pattern)
    {
        ArgumentNullException.ThrowIfNull(pattern);

        if (!patterns.TryAdd(pattern.Id, pattern))
        {
            throw new InvalidOperationException(
                $"Pattern '{pattern.Id}' is already selected.");
        }
    }

    public void AddTransport(TransportDescriptor transport)
    {
        ArgumentNullException.ThrowIfNull(transport);

        if (!transports.TryAdd(transport.Id, transport))
        {
            throw new InvalidOperationException(
                $"Transport '{transport.Id}' is already selected.");
        }
    }

    public void AddTechnology(TechnologyDescriptor technology)
    {
        ArgumentNullException.ThrowIfNull(technology);

        RegisterTechnology(technology);
        SelectTechnology(technology.Id);
    }

    public void RegisterTechnology(TechnologyDescriptor technology)
    {
        ArgumentNullException.ThrowIfNull(technology);

        if (technologyCatalog.TryGetValue(technology.Id, out var existing))
        {
            if (AreEquivalent(existing, technology))
            {
                return;
            }

            throw new InvalidOperationException(
                $"Technology '{technology.Id}' is already registered with a different definition.");
        }

        technologyCatalog.Add(technology.Id, technology);
    }

    public void RegisterTechnologies(IEnumerable<TechnologyDescriptor> technologies)
    {
        ArgumentNullException.ThrowIfNull(technologies);

        foreach (var technology in technologies)
        {
            RegisterTechnology(technology);
        }
    }

    public void SelectTechnology(string technology)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(technology);

        selectedTechnologies.Add(technology.Trim());
    }

    public void UseDataSelection(DataSelection selection)
    {
        dataSelection = selection ?? throw new ArgumentNullException(nameof(selection));
    }

    public void UseDatabaseSelection(DatabaseTopologySelection selection)
    {
        databaseSelection = selection ?? throw new ArgumentNullException(nameof(selection));
    }

    public void UseIdentitySelection(IdentitySelection selection)
    {
        identitySelection = selection ?? throw new ArgumentNullException(nameof(selection));
    }

    public void UseTenancySelection(TenancySelection selection)
    {
        tenancySelection = selection ?? throw new ArgumentNullException(nameof(selection));
    }

    public void UseAuditSelection(AuditSelection selection)
    {
        auditSelection = selection ?? throw new ArgumentNullException(nameof(selection));
    }

    public void UseMessagingSelection(MessagingSelection selection)
    {
        messagingSelection = selection ?? throw new ArgumentNullException(nameof(selection));
    }

    public void UseResilienceSelection(ResilienceSelection selection)
    {
        resilienceSelection = selection ?? throw new ArgumentNullException(nameof(selection));
    }

    public IReadOnlyList<TechnologyDescriptor> GetTechnologyCatalog()
    {
        return technologyCatalog.Values
            .OrderBy(technology => technology.Kind)
            .ThenBy(technology => technology.Id, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public AppProfile Build()
    {
        var selected = new Dictionary<string, PatternDescriptor>(StringComparer.OrdinalIgnoreCase);

        foreach (var pattern in blueprint.Patterns)
        {
            selected.Add(pattern.Id, pattern);
        }

        foreach (var pattern in patterns.Values)
        {
            selected.TryAdd(pattern.Id, pattern);
        }

        var resolvedTechnologies = selectedTechnologies
            .Select(ResolveTechnology)
            .GroupBy(technology => technology.Id, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToDictionary(technology => technology.Id, StringComparer.OrdinalIgnoreCase);

        Validate(selected, transports, resolvedTechnologies);
        AppProfileSelectionValidator.Validate(
            dataSelection,
            databaseSelection,
            identitySelection,
            tenancySelection,
            auditSelection,
            messagingSelection,
            resilienceSelection,
            selected,
            resolvedTechnologies);

        var orderedPatterns = selected.Values
            .OrderBy(pattern => pattern.Kind)
            .ThenBy(pattern => pattern.Id, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var orderedTechnologies = resolvedTechnologies.Values
            .OrderBy(technology => technology.Kind)
            .ThenBy(technology => technology.Id, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var orderedTransports = transports.Values
            .OrderBy(transport => transport.Id, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new AppProfile(
            blueprintId: blueprint.Id,
            blueprintDisplayName: blueprint.DisplayName,
            blueprintDescription: blueprint.Description,
            patterns: orderedPatterns,
            scaffold: BuildScaffold(orderedPatterns, orderedTransports, orderedTechnologies),
            technologies: orderedTechnologies,
            transports: orderedTransports,
            data: dataSelection,
            databases: databaseSelection,
            identity: identitySelection,
            tenancy: tenancySelection,
            audit: auditSelection,
            messaging: messagingSelection,
            resilience: resilienceSelection);
    }

    private static void Validate(
        Dictionary<string, PatternDescriptor> selectedPatterns,
        Dictionary<string, TransportDescriptor> selectedTransports,
        Dictionary<string, TechnologyDescriptor> selectedTechnologies)
    {
        foreach (var pattern in selectedPatterns.Values)
        {
            foreach (var requiredPatternId in pattern.Requires)
            {
                if (!selectedPatterns.ContainsKey(requiredPatternId))
                {
                    throw new InvalidOperationException(
                        $"Pattern '{pattern.Id}' requires '{requiredPatternId}', but that pattern was not selected.");
                }
            }

            foreach (var conflictingPatternId in pattern.ConflictsWith)
            {
                if (selectedPatterns.ContainsKey(conflictingPatternId))
                {
                    throw new InvalidOperationException(
                        $"Pattern '{pattern.Id}' conflicts with '{conflictingPatternId}'.");
                }
            }
        }

        foreach (var technology in selectedTechnologies.Values)
        {
            foreach (var requiredPatternId in technology.RequiresPatterns)
            {
                if (!selectedPatterns.ContainsKey(requiredPatternId))
                {
                    throw new InvalidOperationException(
                        $"Technology '{technology.Id}' requires pattern '{requiredPatternId}', but that pattern was not selected.");
                }
            }

            foreach (var requiredTransportId in technology.RequiresTransports)
            {
                if (!selectedTransports.ContainsKey(requiredTransportId))
                {
                    throw new InvalidOperationException(
                        $"Technology '{technology.Id}' requires transport '{requiredTransportId}', but that transport was not selected.");
                }
            }

            foreach (var requiredTechnologyId in technology.RequiresTechnologies)
            {
                if (!selectedTechnologies.ContainsKey(requiredTechnologyId))
                {
                    throw new InvalidOperationException(
                        $"Technology '{technology.Id}' requires technology '{requiredTechnologyId}', but that technology was not selected.");
                }
            }

            foreach (var conflictingTechnologyId in technology.ConflictsWith)
            {
                if (selectedTechnologies.ContainsKey(conflictingTechnologyId))
                {
                    throw new InvalidOperationException(
                        $"Technology '{technology.Id}' conflicts with '{conflictingTechnologyId}'.");
                }
            }
        }
    }

    private TechnologyDescriptor ResolveTechnology(string value)
    {
        if (technologyCatalog.TryGetValue(value, out var technology))
        {
            return technology;
        }

        var normalizedKey = NormalizeTechnologyKey(value);
        technology = technologyCatalog.Values.FirstOrDefault(candidate =>
            string.Equals(NormalizeTechnologyKey(candidate.Id), normalizedKey, StringComparison.Ordinal) ||
            string.Equals(NormalizeTechnologyKey(candidate.DisplayName), normalizedKey, StringComparison.Ordinal) ||
            candidate.Aliases.Any(alias =>
                string.Equals(NormalizeTechnologyKey(alias), normalizedKey, StringComparison.Ordinal)));

        if (technology is not null)
        {
            return technology;
        }

        if (BuiltInTechnologies.TryResolve(value, out technology))
        {
            return technology;
        }

        throw new InvalidOperationException(
            $"Technology '{value}' is not supported. Supported technologies: {string.Join(", ", GetTechnologyCatalog().Select(item => item.DisplayName))}.");
    }

    private ScaffoldPlan? BuildScaffold(
        IReadOnlyList<PatternDescriptor> selectedPatterns,
        IReadOnlyList<TransportDescriptor> selectedTransports,
        IReadOnlyList<TechnologyDescriptor> selectedTechnologies)
    {
        if (blueprint.Scaffold is null)
        {
            return null;
        }

        var conventions = blueprint.Scaffold.Conventions.ToList();
        foreach (var transport in selectedTransports)
        {
            if (transport.Metadata.TryGetValue("aspnet.registration", out var registration))
            {
                conventions.Add($"{transport.DisplayName}: {registration}");
                continue;
            }

            conventions.Add(
                $"Register the {transport.DisplayName} adapter in startup when '{transport.Id}' is selected in Engine:Transports.");
        }

        foreach (var technology in selectedTechnologies)
        {
            foreach (var guidance in technology.Guidance)
            {
                conventions.Add($"{technology.DisplayName}: {guidance}");
            }
        }

        return new ScaffoldPlan(
            id: blueprint.Scaffold.Id,
            displayName: blueprint.Scaffold.DisplayName,
            description: blueprint.Scaffold.Description,
            projects: blueprint.Scaffold.Projects
                .Select(project => CloneProject(project, selectedPatterns, selectedTransports, selectedTechnologies))
                .ToArray(),
            folders: blueprint.Scaffold.Folders
                .Select(CloneFolder)
                .ToArray(),
            conventions: conventions,
            metadata: blueprint.Scaffold.Metadata);
    }

    private ScaffoldProject CloneProject(
        ScaffoldProject project,
        IReadOnlyList<PatternDescriptor> selectedPatterns,
        IReadOnlyList<TransportDescriptor> selectedTransports,
        IReadOnlyList<TechnologyDescriptor> selectedTechnologies)
    {
        var packages = project.Packages.ToList();

        if (project.Metadata.TryGetValue("hostKind", out var hostKind) &&
            string.Equals(hostKind, "aspnet-core", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var transport in selectedTransports)
            {
                if (transport.Metadata.TryGetValue("aspnet.adapterPackage", out var package) &&
                    !string.IsNullOrWhiteSpace(package))
                {
                    packages.Add(package);
                }
            }
        }

        if (string.Equals(project.Role, ProjectRoles.Host, StringComparison.OrdinalIgnoreCase))
        {
            foreach (var technology in selectedTechnologies)
            {
                foreach (var package in technology.PackageHints)
                {
                    packages.Add(package);
                }
            }

            var includeDataPack = ShouldHintDataPack(selectedPatterns);
            var includeSfidPack = ShouldHintSfidPack(selectedPatterns);
            var includeIdentityPack = ShouldHintIdentityPack(selectedTechnologies);
            var includeMultiTenancyPack = ShouldHintMultiTenancyPack(selectedTechnologies);
            var includeAuditPack = ShouldHintAuditPack();
            var includeEventingPack = ShouldHintEventingPack(selectedTechnologies);
            var includeWolverinePack = ShouldHintWolverinePack(selectedTechnologies);

            if (includeDataPack)
            {
                packages.Add("Cephalon.Data");
            }

            if (includeSfidPack)
            {
                packages.Add("Cephalon.Ids.Sfid");
            }

            if (includeIdentityPack)
            {
                packages.Add("Cephalon.Identity");
            }

            if (includeMultiTenancyPack)
            {
                packages.Add("Cephalon.MultiTenancy");
            }

            if (includeAuditPack)
            {
                packages.Add("Cephalon.Audit");
            }

            if (includeEventingPack)
            {
                packages.Add("Cephalon.Eventing");
            }

            if (includeWolverinePack)
            {
                packages.Add("Cephalon.Eventing.Wolverine");
            }

            if (includeIdentityPack &&
                project.Metadata.TryGetValue("hostKind", out hostKind) &&
                string.Equals(hostKind, "aspnet-core", StringComparison.OrdinalIgnoreCase))
            {
                packages.Add("Cephalon.Identity.AspNetCore");
            }
        }

        return new ScaffoldProject(
            id: project.Id,
            nameTemplate: project.NameTemplate,
            pathTemplate: project.PathTemplate,
            scope: project.Scope,
            role: project.Role,
            template: project.Template,
            dependsOn: project.DependsOn,
            packages: packages.Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            metadata: project.Metadata);
    }

    private static ScaffoldFolder CloneFolder(ScaffoldFolder folder)
    {
        return new ScaffoldFolder(
            pathTemplate: folder.PathTemplate,
            purpose: folder.Purpose,
            scope: folder.Scope,
            projectId: folder.ProjectId,
            metadata: folder.Metadata);
    }

    private bool ShouldHintDataPack(IReadOnlyList<PatternDescriptor> selectedPatterns)
    {
        return dataSelection.HasValues ||
            HasPattern(selectedPatterns, "cqrs") ||
            HasPattern(selectedPatterns, "outbox");
    }

    private bool ShouldHintSfidPack(IReadOnlyList<PatternDescriptor> selectedPatterns)
    {
        return !string.IsNullOrWhiteSpace(dataSelection.IdGenerator) ||
            ShouldHintDataPack(selectedPatterns);
    }

    private bool ShouldHintIdentityPack(IReadOnlyList<TechnologyDescriptor> selectedTechnologies)
    {
        return identitySelection.HasValues ||
            HasTechnology(selectedTechnologies, "identity-access");
    }

    private bool ShouldHintMultiTenancyPack(IReadOnlyList<TechnologyDescriptor> selectedTechnologies)
    {
        return tenancySelection.HasValues ||
            HasTechnology(selectedTechnologies, "multi-tenancy");
    }

    private bool ShouldHintAuditPack()
    {
        return auditSelection.Enabled == true;
    }

    private bool ShouldHintEventingPack(IReadOnlyList<TechnologyDescriptor> selectedTechnologies)
    {
        return messagingSelection.HasValues ||
            HasTechnology(selectedTechnologies, "event-driven-integration");
    }

    private bool ShouldHintWolverinePack(IReadOnlyList<TechnologyDescriptor> selectedTechnologies)
    {
        var provider = ResolveGeneratedMessagingProvider(selectedTechnologies);
        return string.Equals(provider, "Wolverine", StringComparison.OrdinalIgnoreCase);
    }

    private string? ResolveGeneratedMessagingProvider(IReadOnlyList<TechnologyDescriptor> selectedTechnologies)
    {
        if (!string.IsNullOrWhiteSpace(messagingSelection.Provider))
        {
            return messagingSelection.Provider;
        }

        return ShouldHintEventingPack(selectedTechnologies)
            ? "Wolverine"
            : null;
    }

    private static bool HasPattern(IReadOnlyList<PatternDescriptor> selectedPatterns, string patternId)
    {
        return selectedPatterns.Any(pattern =>
            string.Equals(pattern.Id, patternId, StringComparison.OrdinalIgnoreCase));
    }

    private static bool HasTechnology(IReadOnlyList<TechnologyDescriptor> selectedTechnologies, string technologyId)
    {
        return selectedTechnologies.Any(technology =>
            string.Equals(technology.Id, technologyId, StringComparison.OrdinalIgnoreCase));
    }

    private static bool AreEquivalent(TechnologyDescriptor left, TechnologyDescriptor right)
    {
        return string.Equals(left.Id, right.Id, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(left.DisplayName, right.DisplayName, StringComparison.Ordinal) &&
            string.Equals(left.Description, right.Description, StringComparison.Ordinal) &&
            left.Kind == right.Kind &&
            left.Aliases.SequenceEqual(right.Aliases, StringComparer.OrdinalIgnoreCase) &&
            left.Tags.SequenceEqual(right.Tags, StringComparer.OrdinalIgnoreCase) &&
            left.RequiresPatterns.SequenceEqual(right.RequiresPatterns, StringComparer.OrdinalIgnoreCase) &&
            left.RequiresTransports.SequenceEqual(right.RequiresTransports, StringComparer.OrdinalIgnoreCase) &&
            left.RequiresTechnologies.SequenceEqual(right.RequiresTechnologies, StringComparer.OrdinalIgnoreCase) &&
            left.ConflictsWith.SequenceEqual(right.ConflictsWith, StringComparer.OrdinalIgnoreCase) &&
            left.PackageHints.SequenceEqual(right.PackageHints, StringComparer.OrdinalIgnoreCase) &&
            left.Guidance.SequenceEqual(right.Guidance, StringComparer.Ordinal) &&
            left.Metadata.Count == right.Metadata.Count &&
            left.Metadata.OrderBy(static pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                .SequenceEqual(
                    right.Metadata.OrderBy(static pair => pair.Key, StringComparer.OrdinalIgnoreCase),
                    KeyValuePairComparer.Instance);
    }

    private static string NormalizeTechnologyKey(string value)
    {
        return new string(value
            .Where(char.IsLetterOrDigit)
            .Select(char.ToUpperInvariant)
            .ToArray());
    }

    private sealed class KeyValuePairComparer : IEqualityComparer<KeyValuePair<string, string>>
    {
        public static KeyValuePairComparer Instance { get; } = new();

        public bool Equals(KeyValuePair<string, string> x, KeyValuePair<string, string> y)
        {
            return string.Equals(x.Key, y.Key, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(x.Value, y.Value, StringComparison.Ordinal);
        }

        public int GetHashCode(KeyValuePair<string, string> obj)
        {
            return HashCode.Combine(
                StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Key),
                StringComparer.Ordinal.GetHashCode(obj.Value));
        }
    }
}
