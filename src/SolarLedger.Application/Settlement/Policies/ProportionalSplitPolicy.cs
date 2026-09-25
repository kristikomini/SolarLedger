namespace SolarLedger.Application.Settlement.Policies;

/// <summary>
/// Splits the incentive between producers and consumers by a fixed ratio. The producer
/// pot is shared in proportion to injection during shared hours; the consumer pot in
/// proportion to withdrawal during shared hours. Prosumers naturally appear on both
/// sides. The two pots are rounded so they still sum to the total to the cent.
/// </summary>
public sealed class ProportionalSplitPolicy : IDistributionPolicy
{
    private readonly decimal _producerShare;

    /// <param name="producerShare">Fraction (0..1) of the incentive going to producers.</param>
    public ProportionalSplitPolicy(decimal producerShare)
    {
        if (producerShare is < 0m or > 1m)
            throw new ArgumentOutOfRangeException(nameof(producerShare));
        _producerShare = producerShare;
    }

    public string Name => $"ProportionalSplit({_producerShare:0.##}/{1 - _producerShare:0.##})";

    public IReadOnlyList<MemberAllocation> Allocate(SettlementContext context)
    {
        var members = context.Members;

        var producerPot = Math.Round(
            context.TotalIncentiveEur * _producerShare, 2, MidpointRounding.AwayFromZero);
        var consumerPot = context.TotalIncentiveEur - producerPot;

        var producerAmounts = MoneyDistribution.Distribute(
            producerPot, members.Select(m => m.InjectedInSharedHoursKwh).ToList());
        var consumerAmounts = MoneyDistribution.Distribute(
            consumerPot, members.Select(m => m.WithdrawnInSharedHoursKwh).ToList());

        return members
            .Select((m, i) => new MemberAllocation(
                m.MemberId,
                m.InjectedInSharedHoursKwh + m.WithdrawnInSharedHoursKwh,
                producerAmounts[i] + consumerAmounts[i]))
            .ToList();
    }
}
