using Cephalon.Abstractions.Behaviors;

namespace Cephalon.Behaviors.Services;

/// <summary>Validates behavior topology descriptors against all registered compatibility rules.</summary>
internal sealed class CompatibilityMatrix
{
    private readonly IEnumerable<IBehaviorCompatibilityRule> _rules;

    /// <summary>Initializes a new instance of <see cref="CompatibilityMatrix"/>.</summary>
    public CompatibilityMatrix(IEnumerable<IBehaviorCompatibilityRule> rules) => _rules = rules;

    /// <summary>Runs all rules over all descriptors and groups violations by severity.</summary>
    public (IReadOnlyList<BehaviorCompatibilityViolation> Errors,
            IReadOnlyList<BehaviorCompatibilityViolation> Warnings,
            IReadOnlyList<BehaviorCompatibilityViolation> Advisories)
        Validate(IEnumerable<BehaviorTopologyDescriptor> descriptors)
    {
        var errors = new List<BehaviorCompatibilityViolation>();
        var warnings = new List<BehaviorCompatibilityViolation>();
        var advisories = new List<BehaviorCompatibilityViolation>();

        foreach (var descriptor in descriptors)
            foreach (var rule in _rules)
            {
                var v = rule.Check(descriptor);
                if (v is null) continue;
                switch (v.Severity)
                {
                    case CompatibilitySeverity.Error:    errors.Add(v);     break;
                    case CompatibilitySeverity.Warning:  warnings.Add(v);   break;
                    case CompatibilitySeverity.Advisory: advisories.Add(v); break;
                }
            }

        return (errors, warnings, advisories);
    }
}
