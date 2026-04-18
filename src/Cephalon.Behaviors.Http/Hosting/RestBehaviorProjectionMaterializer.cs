using Cephalon.Abstractions.Modules;
using Cephalon.Abstractions.Transports;
using Cephalon.AspNetCore.Hosting;
using Cephalon.AspNetCore.Transports.Rest;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cephalon.Behaviors.Http.Hosting;

internal static class RestBehaviorProjectionMaterializer
{
    internal static void MapModule(
        IEndpointRouteBuilder endpoints,
        IModule module,
        RestBehaviorModuleProjection projection)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(module);
        ArgumentNullException.ThrowIfNull(projection);

        var configuration = endpoints.ServiceProvider.GetRequiredService<IConfiguration>();
        var apiRoutesOptions = ApiRoutesOptions.FromConfiguration(configuration);
        var candidateRegistry = endpoints.ServiceProvider.GetService<IRestEndpointCandidateRuntimeRegistry>();
        var loggerFactory = endpoints.ServiceProvider.GetService<ILoggerFactory>();
        var logger = loggerFactory?.CreateLogger(typeof(RestBehaviorProjectionMaterializer).FullName ?? "Cephalon.Behaviors.Http.Hosting.RestBehaviorProjectionMaterializer");
        if (logger is not null && !logger.IsEnabled(LogLevel.Information))
        {
            logger = null;
        }
        var governanceOptions = endpoints.ServiceProvider.GetService<RestApiGovernanceOptions>()
            ?? RestApiGovernanceOptions.FromConfiguration(configuration);
        var candidates = RestBehaviorProjectionCandidateResolver.ResolveCandidates(
            module.Descriptor,
            apiRoutesOptions,
            projection.Groups,
            governanceOptions.Suppressions,
            governanceOptions.Overrides,
            governanceOptions.AuthoringPolicies);

        for (var groupIndex = 0; groupIndex < projection.Groups.Count; groupIndex++)
        {
            var publishedCandidates = candidates
                .Where(candidate =>
                    candidate.GroupIndex == groupIndex &&
                    candidate.Candidate.Status == RestEndpointCandidateStatus.Published)
                .ToArray();
            if (publishedCandidates.Length == 0)
            {
                continue;
            }

            MapGroup(endpoints, module, projection.Groups[groupIndex], publishedCandidates, apiRoutesOptions);
        }

        RegisterCandidates(candidateRegistry, logger, endpoints, candidates);
    }

    internal static void MapGroup(
        IEndpointRouteBuilder endpoints,
        IModule module,
        RestBehaviorRouteGroupProjection projection,
        IReadOnlyList<ResolvedRestBehaviorEndpointProjectionCandidate> publishedCandidates,
        ApiRoutesOptions apiRoutesOptions)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(module);
        ArgumentNullException.ThrowIfNull(projection);
        ArgumentNullException.ThrowIfNull(publishedCandidates);
        ArgumentNullException.ThrowIfNull(apiRoutesOptions);

        foreach (var routeGroup in publishedCandidates
                     .GroupBy(candidate => ResolvePublishedMaterializationIdentity(candidate.Candidate))
                     .OrderBy(static group => group.Key.RouteGroupPrefix, StringComparer.OrdinalIgnoreCase)
                     .ThenBy(static group => group.Key.OpenApiDocumentName, StringComparer.OrdinalIgnoreCase)
                     .ThenBy(static group => group.Key.TagName, StringComparer.OrdinalIgnoreCase))
        {
            var firstCandidate = routeGroup.First();
            var group = endpoints.MapBehaviorRestGroup(
                module,
                ResolveMaterializationGroupPrefix(routeGroup.Key.RouteGroupPrefix, apiRoutesOptions.RestPrefix));
            group.UseRuntimeSourceKind(RestEndpointRuntimeMetadata.ModuleDslSourceKind);
            group.UseRuntimeAuthoringStyle(RestEndpointRuntimeMetadata.BehaviorModuleDslAuthoringStyle);
            if (!string.IsNullOrWhiteSpace(routeGroup.Key.OpenApiDocumentName))
            {
                group.WithOpenApiDocumentName(routeGroup.Key.OpenApiDocumentName);
            }

            if (!string.IsNullOrWhiteSpace(routeGroup.Key.TagName))
            {
                group.WithTagName(routeGroup.Key.TagName);
            }
            else if (!string.IsNullOrWhiteSpace(projection.TagName))
            {
                group.WithTagName(projection.TagName);
            }

            if (projection.HasExplicitTagDescription)
            {
                group.WithTagDescription(projection.TagDescription);
            }

            if (firstCandidate.Candidate.ProjectedEndpoint.ApiVersionMajor is int apiVersionMajor)
            {
                group.ApiVersion(apiVersionMajor);
            }

            foreach (var convention in projection.GroupConventions)
            {
                convention(group.Routes);
            }

            foreach (var candidate in routeGroup)
            {
                group.UseRuntimeCandidateId(candidate.Candidate.Id);
                group.UseRuntimeOriginalProjection(candidate.Candidate.OriginalProjection);
                group.UseRuntimeOriginalEndpointMetadata(
                    candidate.Candidate.ProjectedEndpoint.OriginalEndpointName,
                    candidate.Candidate.ProjectedEndpoint.OriginalSummary,
                    candidate.Candidate.ProjectedEndpoint.OriginalDescription);
                group.UseRuntimeMatchedOverrideIds(candidate.Candidate.MatchedOverrideIds);
                group.UseRuntimeSelectedOverride(
                    candidate.Candidate.SelectedOverrideId,
                    candidate.Candidate.SelectedOverrideActionKinds);
                group.UseRuntimeSkippedGovernanceRuleIds(
                    candidate.Candidate.SkippedSuppressionIds,
                    candidate.Candidate.SkippedOverrideIds);
                group.UseRuntimeOverrideSelectionBasis(candidate.Candidate.OverrideSelectionBasis);
                var builder = candidate.EffectiveEndpointProjection.Apply(group);
                var sourceCapabilityCapture = CaptureSourceCapability(builder);
                var sourceDocumentationCapture = CaptureSourceDocumentation(builder);
                ApplyResolvedEndpointName(
                    builder,
                    candidate.Candidate.ProjectedEndpoint.EndpointName,
                    sourceDocumentationCapture.EndpointName);
                ApplyRequiredCapabilityOverride(builder, candidate.AppliedCapabilityOverride);
                ApplyEndpointMetadataOverride(builder, candidate.AppliedMetadataOverride);
                ApplyPublishedOverrideProvenance(
                    builder,
                    candidate,
                    sourceCapabilityCapture,
                    sourceDocumentationCapture);
            }
        }
    }

    private static CapturedEndpointCapabilityState CaptureSourceCapability(RouteHandlerBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var capture = new CapturedEndpointCapabilityState();
        builder.Add(endpointBuilder =>
        {
            var sourceRequiredCapabilityKey = RestEndpointRuntimeMetadata.ResolveEffectiveRequiredCapabilityKey(
                endpointBuilder.Metadata.OfType<RestEndpointCapabilityMetadata>());
            capture.RequiredCapabilityKey = sourceRequiredCapabilityKey;
            endpointBuilder.Metadata.Add(new RestEndpointSourceCapabilityMetadata(sourceRequiredCapabilityKey));
        });

        return capture;
    }

    private static CapturedEndpointDocumentationState CaptureSourceDocumentation(RouteHandlerBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var capture = new CapturedEndpointDocumentationState();
        builder.Add(endpointBuilder =>
        {
            var sourceEndpointName = endpointBuilder.Metadata.OfType<EndpointNameMetadata>().LastOrDefault()?.EndpointName;
            var sourceSummary = endpointBuilder.Metadata.OfType<IEndpointSummaryMetadata>().LastOrDefault()?.Summary;
            var sourceDescription = endpointBuilder.Metadata.OfType<IEndpointDescriptionMetadata>().LastOrDefault()?.Description;
            capture.EndpointName = sourceEndpointName;
            capture.Summary = sourceSummary;
            capture.Description = sourceDescription;
            endpointBuilder.Metadata.Add(new RestEndpointSourceDocumentationMetadata(
                sourceEndpointName,
                sourceSummary,
                sourceDescription));
        });

        return capture;
    }

    private static void ApplyResolvedEndpointName(
        RouteHandlerBuilder builder,
        string? endpointName,
        string? sourceEndpointName)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (string.IsNullOrWhiteSpace(endpointName) ||
            string.Equals(endpointName, sourceEndpointName, StringComparison.Ordinal))
        {
            return;
        }

        builder.WithName(endpointName);
    }

    private static void RegisterCandidates(
        IRestEndpointCandidateRuntimeRegistry? candidateRegistry,
        ILogger? logger,
        IEndpointRouteBuilder endpoints,
        IReadOnlyList<ResolvedRestBehaviorEndpointProjectionCandidate> candidates)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(candidates);

        if (candidates.Count == 0 || (candidateRegistry is null && logger is null))
        {
            return;
        }

        if (candidateRegistry is null)
        {
            if (logger is not null)
            {
                LogGovernanceOutcomes(
                    logger,
                    candidates.Select(static candidate => candidate.Candidate).ToArray());
            }

            return;
        }

        var publishedCandidateStates = ResolvePublishedCandidateStates(endpoints, candidates);
        var registeredCandidates = candidates
            .Select(candidate => CreateRegisteredCandidateDescriptor(candidate, publishedCandidateStates))
            .ToArray();
        foreach (var candidate in registeredCandidates)
        {
            candidateRegistry.Register(candidate);
        }

        if (logger is not null)
        {
            LogGovernanceOutcomes(logger, registeredCandidates);
        }
    }

    private static void LogGovernanceOutcomes(
        ILogger logger,
        RestEndpointCandidateRuntimeDescriptor[] candidates)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(candidates);

        if (candidates.Length == 0)
        {
            return;
        }

        var candidatesById = candidates.ToDictionary(static candidate => candidate.Id, StringComparer.OrdinalIgnoreCase);
        foreach (var candidate in candidates)
        {
            var behaviorId = candidate.ProjectedEndpoint.BehaviorId ?? "(unknown)";
            if (candidate.SkippedSuppressionIds.Count > 0 ||
                candidate.SkippedOverrideIds.Count > 0)
            {
                RestBehaviorGovernanceLoggerMessages.LogGovernanceSkipped(
                    logger,
                    candidate.Id,
                    behaviorId,
                    candidate.AuthoringStyle,
                    JoinIdentifiers(candidate.SkippedSuppressionIds),
                    JoinIdentifiers(candidate.SkippedOverrideIds));
            }

            if (!string.IsNullOrWhiteSpace(candidate.SuppressedBySuppressionId))
            {
                RestBehaviorGovernanceLoggerMessages.LogGovernanceSuppressed(
                    logger,
                    candidate.Id,
                    behaviorId,
                    candidate.AuthoringStyle,
                    candidate.SuppressedBySuppressionId,
                    JoinSelectionBasis(candidate.SuppressionSelectionBasis),
                    JoinIdentifiers(candidate.MatchedSuppressionIds));
            }
            else if (candidate.SuppressedByAuthoringPolicyKind is { } authoringPolicySuppressionKind)
            {
                RestBehaviorGovernanceLoggerMessages.LogAuthoringPolicySuppressed(
                    logger,
                    candidate.Id,
                    behaviorId,
                    candidate.AuthoringStyle,
                    authoringPolicySuppressionKind.GetWireName(),
                    candidate.SuppressionReason ?? "(none)");
            }
            else if (!string.IsNullOrWhiteSpace(candidate.SuppressedByCandidateId) &&
                     candidatesById.TryGetValue(candidate.SuppressedByCandidateId, out var winningCandidate))
            {
                RestBehaviorGovernanceLoggerMessages.LogPrecedenceSuppressed(
                    logger,
                    candidate.Id,
                    behaviorId,
                    candidate.AuthoringStyle,
                    winningCandidate.Id,
                    winningCandidate.AuthoringStyle);
            }

            if (!string.IsNullOrWhiteSpace(candidate.AppliedOverrideId))
            {
                RestBehaviorGovernanceLoggerMessages.LogOverrideApplied(
                    logger,
                    candidate.Id,
                    behaviorId,
                    candidate.AppliedOverrideId,
                    JoinSelectionBasis(candidate.OverrideSelectionBasis),
                    JoinActionKinds(candidate.SelectedOverrideActionKinds),
                    JoinActionKinds(candidate.AppliedOverrideActionKinds),
                    candidate.ProjectedEndpoint.RoutePattern);
            }
            else if (candidate.MatchedOverrideIds.Count > 0)
            {
                RestBehaviorGovernanceLoggerMessages.LogOverrideNoOp(
                    logger,
                    candidate.Id,
                    behaviorId,
                    candidate.SelectedOverrideId ?? "(none)",
                    JoinIdentifiers(candidate.MatchedOverrideIds),
                    JoinSelectionBasis(candidate.OverrideSelectionBasis),
                    JoinActionKinds(candidate.SelectedOverrideActionKinds),
                    JoinActionKinds(candidate.AppliedOverrideActionKinds));
            }

            if (candidate.ProjectedEndpoint.BindingFallbackMode is { } bindingFallbackMode &&
                candidate.OriginalProjection.BindingFallbackMode != bindingFallbackMode)
            {
                RestBehaviorGovernanceLoggerMessages.LogBindingFallbackPreserved(
                    logger,
                    candidate.Id,
                    behaviorId,
                    bindingFallbackMode.GetWireName(),
                    JoinIdentifiers(candidate.MatchedOverrideIds));
            }
        }
    }

    private static string JoinIdentifiers(IReadOnlyList<string> identifiers)
    {
        ArgumentNullException.ThrowIfNull(identifiers);

        return identifiers.Count == 0
            ? "(none)"
            : string.Join(", ", identifiers);
    }

    private static string JoinActionKinds(IReadOnlyList<RestEndpointOverrideActionKind> actionKinds)
    {
        ArgumentNullException.ThrowIfNull(actionKinds);

        return actionKinds.Count == 0
            ? "(none)"
            : string.Join(", ", actionKinds.Select(static item => item.GetWireName()));
    }

    private static string JoinSelectionBasis(RestEndpointGovernanceRuleSelectionBasis? selectionBasis)
    {
        return selectionBasis.HasValue
            ? selectionBasis.Value.GetWireName()
            : "(none)";
    }

    private static Dictionary<string, MaterializedPublishedCandidateState> ResolvePublishedCandidateStates(
        IEndpointRouteBuilder endpoints,
        IReadOnlyList<ResolvedRestBehaviorEndpointProjectionCandidate> candidates)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(candidates);

        var publishedCandidates = candidates
            .Where(static candidate => candidate.Candidate.Status == RestEndpointCandidateStatus.Published)
            .ToArray();
        if (publishedCandidates.Length == 0)
        {
            return new Dictionary<string, MaterializedPublishedCandidateState>(StringComparer.OrdinalIgnoreCase);
        }

        var publishedCandidateIds = publishedCandidates
            .Select(static candidate => candidate.Candidate.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var states = new Dictionary<string, MaterializedPublishedCandidateState>(StringComparer.OrdinalIgnoreCase);
        var matchingEndpoints = endpoints.DataSources
            .SelectMany(static dataSource => dataSource.Endpoints)
            .OfType<RouteEndpoint>()
            .Where(endpoint =>
            {
                var metadata = endpoint.Metadata.GetMetadata<RestBehaviorEndpointMetadata>();
                return !string.IsNullOrWhiteSpace(metadata?.CandidateId) &&
                       publishedCandidateIds.Contains(metadata.CandidateId);
            })
            .GroupBy(
                endpoint => endpoint.Metadata.GetMetadata<RestBehaviorEndpointMetadata>()!.CandidateId!,
                StringComparer.OrdinalIgnoreCase);

        foreach (var group in matchingEndpoints)
        {
            var materializedEndpoints = group.ToArray();
            if (materializedEndpoints.Length != 1)
            {
                throw new InvalidOperationException(
                    $"REST behavior candidate '{group.Key}' materialized {materializedEndpoints.Length} endpoints while runtime candidate reconciliation expected exactly one.");
            }

            states[group.Key] = CreateMaterializedPublishedCandidateState(materializedEndpoints[0]);
        }

        foreach (var candidate in publishedCandidates)
        {
            if (!states.ContainsKey(candidate.Candidate.Id))
            {
                throw new InvalidOperationException(
                    $"Published REST behavior candidate '{candidate.Candidate.Id}' did not produce a materialized endpoint for runtime candidate reconciliation.");
            }
        }

        return states;
    }

    private static MaterializedPublishedCandidateState CreateMaterializedPublishedCandidateState(RouteEndpoint endpoint)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        return new MaterializedPublishedCandidateState(
            endpoint.Metadata.GetMetadata<RestEndpointAppliedOverrideMetadata>()?.OverrideId,
            endpoint.Metadata.GetMetadata<RestEndpointAppliedOverrideMetadata>()?.ActionKinds);
    }

    private static RestEndpointCandidateRuntimeDescriptor CreateRegisteredCandidateDescriptor(
        ResolvedRestBehaviorEndpointProjectionCandidate candidate,
        IReadOnlyDictionary<string, MaterializedPublishedCandidateState> publishedCandidateStates)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(publishedCandidateStates);

        var appliedOverrideId = ResolveRegisteredAppliedOverrideId(candidate, publishedCandidateStates);
        var appliedOverrideActionKinds = ResolveRegisteredAppliedOverrideActionKinds(candidate, publishedCandidateStates);
        return new RestEndpointCandidateRuntimeDescriptor(
            candidate.Candidate.Id,
            candidate.Candidate.ProjectedEndpoint,
            candidate.Candidate.OriginalProjection,
            candidate.Candidate.AuthoringStyle,
            candidate.Candidate.PrecedenceRank,
            candidate.Candidate.Status,
            suppressedByCandidateId: candidate.Candidate.SuppressedByCandidateId,
            suppressedBySuppressionId: candidate.Candidate.SuppressedBySuppressionId,
            appliedOverrideId: appliedOverrideId,
            matchedSuppressionIds: candidate.Candidate.MatchedSuppressionIds,
            matchedOverrideIds: candidate.Candidate.MatchedOverrideIds,
            suppressionReason: candidate.Candidate.SuppressionReason,
            suppressedByAuthoringPolicyKind: candidate.Candidate.SuppressedByAuthoringPolicyKind,
            selectedOverrideId: candidate.Candidate.SelectedOverrideId,
            suppressionSelectionBasis: candidate.Candidate.SuppressionSelectionBasis,
            overrideSelectionBasis: candidate.Candidate.OverrideSelectionBasis,
            skippedSuppressionIds: candidate.Candidate.SkippedSuppressionIds,
            skippedOverrideIds: candidate.Candidate.SkippedOverrideIds,
            selectedOverrideActionKinds: candidate.Candidate.SelectedOverrideActionKinds,
            appliedOverrideActionKinds: appliedOverrideActionKinds);
    }

    private static string? ResolveRegisteredAppliedOverrideId(
        ResolvedRestBehaviorEndpointProjectionCandidate candidate,
        IReadOnlyDictionary<string, MaterializedPublishedCandidateState> publishedCandidateStates)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(publishedCandidateStates);

        if (candidate.Candidate.Status != RestEndpointCandidateStatus.Published)
        {
            return candidate.Candidate.AppliedOverrideId;
        }

        if (!publishedCandidateStates.TryGetValue(candidate.Candidate.Id, out var publishedCandidateState))
        {
            throw new InvalidOperationException(
                $"Published REST behavior candidate '{candidate.Candidate.Id}' is missing the materialized endpoint state required for runtime candidate reconciliation.");
        }

        return publishedCandidateState.AppliedOverrideId;
    }

    private static IReadOnlyList<RestEndpointOverrideActionKind> ResolveRegisteredAppliedOverrideActionKinds(
        ResolvedRestBehaviorEndpointProjectionCandidate candidate,
        IReadOnlyDictionary<string, MaterializedPublishedCandidateState> publishedCandidateStates)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(publishedCandidateStates);

        if (candidate.Candidate.Status != RestEndpointCandidateStatus.Published)
        {
            return candidate.Candidate.AppliedOverrideActionKinds;
        }

        if (!publishedCandidateStates.TryGetValue(candidate.Candidate.Id, out var publishedCandidateState))
        {
            throw new InvalidOperationException(
                $"Published REST behavior candidate '{candidate.Candidate.Id}' is missing the materialized endpoint state required for runtime candidate reconciliation.");
        }

        return publishedCandidateState.AppliedOverrideActionKinds ?? [];
    }

    private static void ApplyRequiredCapabilityOverride(
        RouteHandlerBuilder builder,
        AppliedRestEndpointCapabilityOverride? capabilityOverride)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (capabilityOverride is null)
        {
            return;
        }

        if (capabilityOverride.ClearRequiredCapability)
        {
            builder.ClearRequiredCapability();
            return;
        }

        builder.RequireCapability(capabilityOverride.RequiredCapabilityKey!);
    }

    private static void ApplyEndpointMetadataOverride(
        RouteHandlerBuilder builder,
        AppliedRestEndpointMetadataOverride? metadataOverride)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (metadataOverride is null)
        {
            return;
        }

        if (metadataOverride.ClearEndpointName ||
            metadataOverride.ClearSummary ||
            metadataOverride.ClearDescription)
        {
            builder.Add(endpointBuilder =>
            {
                if (metadataOverride.ClearEndpointName)
                {
                    RemoveMetadata<EndpointNameMetadata>(endpointBuilder.Metadata);
                }

                if (metadataOverride.ClearSummary)
                {
                    RemoveMetadata<IEndpointSummaryMetadata>(endpointBuilder.Metadata);
                }

                if (metadataOverride.ClearDescription)
                {
                    RemoveMetadata<IEndpointDescriptionMetadata>(endpointBuilder.Metadata);
                }

                RemoveMetadata<RestEndpointClearedMetadataState>(endpointBuilder.Metadata);
                endpointBuilder.Metadata.Add(new RestEndpointClearedMetadataState(
                    metadataOverride.ClearEndpointName,
                    metadataOverride.ClearSummary,
                    metadataOverride.ClearDescription));
            });
        }

        if (!string.IsNullOrWhiteSpace(metadataOverride.EndpointName))
        {
            builder.WithName(metadataOverride.EndpointName);
        }

        if (!string.IsNullOrWhiteSpace(metadataOverride.Summary))
        {
            builder.WithSummary(metadataOverride.Summary);
        }

        if (!string.IsNullOrWhiteSpace(metadataOverride.Description))
        {
            builder.WithDescription(metadataOverride.Description);
        }
    }

    private static void RemoveMetadata<TMetadata>(IList<object> metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        for (var index = metadata.Count - 1; index >= 0; index--)
        {
            if (metadata[index] is TMetadata)
            {
                metadata.RemoveAt(index);
            }
        }
    }

    private static void ApplyPublishedOverrideProvenance(
        RouteHandlerBuilder builder,
        ResolvedRestBehaviorEndpointProjectionCandidate candidate,
        CapturedEndpointCapabilityState sourceCapabilityCapture,
        CapturedEndpointDocumentationState sourceDocumentationCapture)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(sourceCapabilityCapture);
        ArgumentNullException.ThrowIfNull(sourceDocumentationCapture);

        if (string.IsNullOrWhiteSpace(candidate.Candidate.AppliedOverrideId))
        {
            return;
        }

        if (HasStructuralOverride(candidate.Candidate))
        {
            builder.WithMetadata(new RestEndpointAppliedOverrideMetadata(
                candidate.Candidate.AppliedOverrideId,
                candidate.Candidate.AppliedOverrideActionKinds));
            return;
        }

        builder.Add(endpointBuilder =>
        {
            var effectiveRequiredCapabilityKey = RestEndpointRuntimeMetadata.ResolveEffectiveRequiredCapabilityKey(
                endpointBuilder.Metadata.OfType<RestEndpointCapabilityMetadata>());
            var capabilityChanged = !string.Equals(
                sourceCapabilityCapture.RequiredCapabilityKey,
                effectiveRequiredCapabilityKey,
                StringComparison.Ordinal);
            var effectiveEndpointName = endpointBuilder.Metadata.OfType<EndpointNameMetadata>().LastOrDefault()?.EndpointName;
            var effectiveSummary = endpointBuilder.Metadata.OfType<IEndpointSummaryMetadata>().LastOrDefault()?.Summary;
            var effectiveDescription = endpointBuilder.Metadata.OfType<IEndpointDescriptionMetadata>().LastOrDefault()?.Description;
            var metadataChanged = !string.Equals(
                                      sourceDocumentationCapture.EndpointName,
                                      effectiveEndpointName,
                                      StringComparison.Ordinal) ||
                                  !string.Equals(
                                      sourceDocumentationCapture.Summary,
                                      effectiveSummary,
                                      StringComparison.Ordinal) ||
                                  !string.Equals(
                                      sourceDocumentationCapture.Description,
                                      effectiveDescription,
                                      StringComparison.Ordinal);
            var evaluatesCapabilityChanges = candidate.AppliedCapabilityOverride is not null;
            var evaluatesDocumentationChanges = candidate.AppliedMetadataOverride is not null;

            if ((evaluatesCapabilityChanges && capabilityChanged) ||
                (evaluatesDocumentationChanges && metadataChanged))
            {
                endpointBuilder.Metadata.Add(new RestEndpointAppliedOverrideMetadata(
                    candidate.Candidate.AppliedOverrideId,
                    candidate.Candidate.AppliedOverrideActionKinds));
            }
        });
    }

    private static bool HasStructuralOverride(RestEndpointCandidateRuntimeDescriptor candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);

        var originalProjection = candidate.OriginalProjection;
        var projectedEndpoint = candidate.ProjectedEndpoint;
        return !string.Equals(originalProjection.Method, projectedEndpoint.Method, StringComparison.Ordinal) ||
               !string.Equals(originalProjection.RoutePattern, projectedEndpoint.RoutePattern, StringComparison.Ordinal) ||
               !string.Equals(originalProjection.RouteGroupPrefix, projectedEndpoint.RouteGroupPrefix, StringComparison.Ordinal) ||
               !string.Equals(originalProjection.RelativePattern, projectedEndpoint.RelativePattern, StringComparison.Ordinal) ||
               originalProjection.ApiVersionMajor != projectedEndpoint.ApiVersionMajor ||
               !string.Equals(originalProjection.OpenApiDocumentName, projectedEndpoint.OpenApiDocumentName, StringComparison.Ordinal) ||
               !string.Equals(originalProjection.TagName, ResolveProjectedTagName(projectedEndpoint), StringComparison.Ordinal) ||
               originalProjection.BindingFallbackMode != projectedEndpoint.BindingFallbackMode ||
               !RestBehaviorBindingDescriptorSetComparer.Equivalent(
                   originalProjection.BindingDescriptors,
                   projectedEndpoint.BindingDescriptors);
    }

    private static string ResolvePublishedRouteGroupPrefix(RestEndpointCandidateRuntimeDescriptor candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);

        if (!string.IsNullOrWhiteSpace(candidate.ProjectedEndpoint.RouteGroupPrefix))
        {
            return candidate.ProjectedEndpoint.RouteGroupPrefix.Trim();
        }

        throw new InvalidOperationException(
            $"REST endpoint candidate '{candidate.Id}' is missing the projected route-group prefix required for endpoint materialization.");
    }

    private static MaterializationGroupIdentity ResolvePublishedMaterializationIdentity(
        RestEndpointCandidateRuntimeDescriptor candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);

        return new MaterializationGroupIdentity(
            ResolvePublishedRouteGroupPrefix(candidate),
            candidate.ProjectedEndpoint.OpenApiDocumentName,
            ResolveProjectedTagName(candidate.ProjectedEndpoint));
    }

    private static string? ResolveProjectedTagName(RestEndpointRuntimeDescriptor projectedEndpoint)
    {
        ArgumentNullException.ThrowIfNull(projectedEndpoint);

        return projectedEndpoint.Tags
            .FirstOrDefault(static tag => !string.IsNullOrWhiteSpace(tag))
            ?.Trim();
    }

    private static string ResolveMaterializationGroupPrefix(
        string publishedRouteGroupPrefix,
        string restPrefix)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(publishedRouteGroupPrefix);
        ArgumentNullException.ThrowIfNull(restPrefix);

        var normalizedPublishedPrefix = NormalizePath(publishedRouteGroupPrefix);
        var normalizedRestPrefix = NormalizePath(restPrefix);
        if (string.Equals(normalizedRestPrefix, "/", StringComparison.Ordinal))
        {
            return normalizedPublishedPrefix;
        }

        if (string.Equals(normalizedPublishedPrefix, normalizedRestPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return "/";
        }

        if (!normalizedPublishedPrefix.StartsWith($"{normalizedRestPrefix}/", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"REST endpoint materialization cannot place projected route-group prefix '{normalizedPublishedPrefix}' beneath active REST root '{normalizedRestPrefix}'.");
        }

        return normalizedPublishedPrefix[normalizedRestPrefix.Length..];
    }

    private static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return "/";
        }

        var normalized = path.Trim();
        if (!normalized.StartsWith('/'))
        {
            normalized = $"/{normalized}";
        }

        return normalized.Length > 1
            ? normalized.TrimEnd('/')
            : normalized;
    }

    private sealed class CapturedEndpointCapabilityState
    {
        internal string? RequiredCapabilityKey { get; set; }
    }

    private sealed class CapturedEndpointDocumentationState
    {
        internal string? EndpointName { get; set; }

        internal string? Summary { get; set; }

        internal string? Description { get; set; }
    }

    private sealed record MaterializedPublishedCandidateState(
        string? AppliedOverrideId,
        IReadOnlyList<RestEndpointOverrideActionKind>? AppliedOverrideActionKinds);

    private sealed record MaterializationGroupIdentity(
        string RouteGroupPrefix,
        string? OpenApiDocumentName,
        string? TagName);
}
