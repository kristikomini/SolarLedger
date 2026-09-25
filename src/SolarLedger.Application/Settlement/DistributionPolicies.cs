using SolarLedger.Application.Settlement.Policies;

namespace SolarLedger.Application.Settlement;

/// <summary>Resolves a policy from a community's stored policy name.</summary>
public static class DistributionPolicies
{
    public static IDistributionPolicy Resolve(string? name) => name switch
    {
        "ProducersConsumers50" => new ProportionalSplitPolicy(0.5m),
        "ProducersConsumers70" => new ProportionalSplitPolicy(0.7m),
        _ => new AllToProducersPolicy() // default
    };
}
