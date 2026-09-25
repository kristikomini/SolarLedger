namespace SolarLedger.Application.Settlement;

/// <summary>
/// How the incentive pot is split among members. Swappable strategy — the community
/// decides. The engine computes the pot and each member's shared-hour energy; the
/// policy only decides who gets what.
/// </summary>
public interface IDistributionPolicy
{
    string Name { get; }

    IReadOnlyList<MemberAllocation> Allocate(SettlementContext context);
}
