namespace SolarLedger.Application.Settlement;

/// <summary>
/// The settlement engine. For each hour it computes the community's shared energy as
/// min(Σ injected, Σ withdrawn) — the quantity produced and consumed within the same
/// hour by the community, which is the only energy the incentive is paid on. It then
/// applies the tariff and hands the pot to a distribution policy.
///
/// Pure: no database, no clock, no randomness. That is what makes the money logic
/// exhaustively testable.
/// </summary>
public sealed class SettlementCalculator
{
    private readonly IDistributionPolicy _policy;

    public SettlementCalculator(IDistributionPolicy policy) => _policy = policy;

    public SettlementResult Calculate(SettlementInput input)
    {
        // Pass 1: community totals per hour.
        var perHour = new Dictionary<DateTime, (decimal injected, decimal withdrawn)>();
        foreach (var member in input.Members)
        {
            foreach (var h in member.Hours)
            {
                perHour.TryGetValue(h.Hour, out var acc);
                perHour[h.Hour] = (acc.injected + h.InjectedKwh, acc.withdrawn + h.WithdrawnKwh);
            }
        }

        // Shared energy per hour = min(injected, withdrawn).
        var hourly = new List<HourlyShareLine>(perHour.Count);
        var sharedHours = new HashSet<DateTime>();
        var totalShared = 0m;

        foreach (var (hour, totals) in perHour.OrderBy(kv => kv.Key))
        {
            var shared = Math.Min(totals.injected, totals.withdrawn);
            if (shared > 0m) sharedHours.Add(hour);
            totalShared += shared;
            hourly.Add(new HourlyShareLine(
                hour, Round3(totals.injected), Round3(totals.withdrawn), Round3(shared)));
        }

        totalShared = Round3(totalShared);

        // Incentive: tariff is €/MWh, energy is kWh.
        var totalIncentive = Math.Round(
            totalShared / 1000m * input.TariffEurPerMwh, 2, MidpointRounding.AwayFromZero);

        // Pass 2: each member's energy that fell in hours where sharing occurred.
        var memberTotals = input.Members
            .Select(m =>
            {
                var inj = 0m;
                var wd = 0m;
                foreach (var h in m.Hours)
                {
                    if (!sharedHours.Contains(h.Hour)) continue;
                    inj += h.InjectedKwh;
                    wd += h.WithdrawnKwh;
                }
                return new MemberSharedTotals(m.MemberId, m.Role, Round3(inj), Round3(wd));
            })
            .ToList();

        var allocations = _policy.Allocate(new SettlementContext(totalIncentive, memberTotals));

        return new SettlementResult(
            totalShared, totalIncentive, _policy.Name, hourly, allocations);
    }

    private static decimal Round3(decimal v) => Math.Round(v, 3, MidpointRounding.AwayFromZero);
}
