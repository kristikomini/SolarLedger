namespace SolarLedger.Application.Settlement.Policies;

/// <summary>
/// The whole incentive goes to producers, split in proportion to the energy each one
/// injected during hours when sharing actually occurred. Pure consumers get nothing.
/// The common CER starting point: the incentive repays the plant investment.
/// </summary>
public sealed class AllToProducersPolicy : IDistributionPolicy
{
    public string Name => "AllToProducers";

    public IReadOnlyList<MemberAllocation> Allocate(SettlementContext context)
    {
        var members = context.Members;
        var weights = members.Select(m => m.InjectedInSharedHoursKwh).ToList();
        var amounts = MoneyDistribution.Distribute(context.TotalIncentiveEur, weights);

        return members
            .Select((m, i) => new MemberAllocation(
                m.MemberId, m.InjectedInSharedHoursKwh, amounts[i]))
            .ToList();
    }
}
