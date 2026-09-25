using SolarLedger.Application.Settlement;
using SolarLedger.Application.Settlement.Policies;
using SolarLedger.Domain.Members;
using Xunit;

namespace SolarLedger.UnitTests;

public class SettlementCalculatorTests
{
    private static readonly DateTime H0 = new(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
    private static DateTime H(int i) => H0.AddHours(i);

    private static MemberEnergy Member(long id, MemberRole role, params HourlyEnergy[] hours)
        => new(id, role, hours);

    private static SettlementCalculator AllToProducers()
        => new(new AllToProducersPolicy());

    [Fact]
    public void Empty_input_yields_zero()
    {
        var result = AllToProducers().Calculate(
            new SettlementInput(100m, Array.Empty<MemberEnergy>()));

        Assert.Equal(0m, result.TotalSharedKwh);
        Assert.Equal(0m, result.TotalIncentiveEur);
        Assert.Empty(result.Members);
    }

    [Fact]
    public void Shared_energy_is_limited_by_withdrawal_when_production_exceeds_it()
    {
        var input = new SettlementInput(100m, new[]
        {
            Member(1, MemberRole.Producer, new HourlyEnergy(H(0), 10m, 0m)),
            Member(2, MemberRole.Consumer, new HourlyEnergy(H(0), 0m, 4m)),
        });

        var result = AllToProducers().Calculate(input);

        Assert.Equal(4m, result.TotalSharedKwh);            // min(10, 4)
        Assert.Equal(0.40m, result.TotalIncentiveEur);      // 4 kWh / 1000 * 100 €/MWh
    }

    [Fact]
    public void Shared_energy_is_limited_by_production_when_consumption_exceeds_it()
    {
        var input = new SettlementInput(100m, new[]
        {
            Member(1, MemberRole.Producer, new HourlyEnergy(H(0), 4m, 0m)),
            Member(2, MemberRole.Consumer, new HourlyEnergy(H(0), 0m, 10m)),
        });

        Assert.Equal(4m, AllToProducers().Calculate(input).TotalSharedKwh);
    }

    [Fact]
    public void No_simultaneity_means_no_shared_energy()
    {
        // Production in hour 0, consumption in hour 1 — never in the same hour.
        var input = new SettlementInput(100m, new[]
        {
            Member(1, MemberRole.Producer, new HourlyEnergy(H(0), 10m, 0m)),
            Member(2, MemberRole.Consumer, new HourlyEnergy(H(1), 0m, 10m)),
        });

        var result = AllToProducers().Calculate(input);
        Assert.Equal(0m, result.TotalSharedKwh);
        Assert.Equal(0m, result.TotalIncentiveEur);
    }

    [Fact]
    public void AllToProducers_pays_producers_and_not_consumers()
    {
        var input = new SettlementInput(100m, new[]
        {
            Member(1, MemberRole.Producer, new HourlyEnergy(H(0), 10m, 0m)),
            Member(2, MemberRole.Consumer, new HourlyEnergy(H(0), 0m, 4m)),
        });

        var result = AllToProducers().Calculate(input);

        var producer = result.Members.Single(m => m.MemberId == 1);
        var consumer = result.Members.Single(m => m.MemberId == 2);

        Assert.Equal(0.40m, producer.IncentiveEur);
        Assert.Equal(0m, consumer.IncentiveEur);
    }

    [Fact]
    public void Proportional_split_pays_both_sides_and_reconciles()
    {
        var input = new SettlementInput(100m, new[]
        {
            Member(1, MemberRole.Producer, new HourlyEnergy(H(0), 10m, 0m)),
            Member(2, MemberRole.Consumer, new HourlyEnergy(H(0), 0m, 4m)),
        });

        var result = new SettlementCalculator(new ProportionalSplitPolicy(0.5m)).Calculate(input);

        var producer = result.Members.Single(m => m.MemberId == 1);
        var consumer = result.Members.Single(m => m.MemberId == 2);

        Assert.Equal(0.20m, producer.IncentiveEur);
        Assert.Equal(0.20m, consumer.IncentiveEur);
        // The split never loses or invents money.
        Assert.Equal(result.TotalIncentiveEur, result.Members.Sum(m => m.IncentiveEur));
    }

    [Fact]
    public void Member_incentives_always_reconcile_to_the_total()
    {
        var input = new SettlementInput(137m, new[]
        {
            Member(1, MemberRole.Producer, new HourlyEnergy(H(0), 7m, 0m), new HourlyEnergy(H(1), 5m, 0m)),
            Member(2, MemberRole.Prosumer, new HourlyEnergy(H(0), 2m, 1m), new HourlyEnergy(H(1), 0m, 3m)),
            Member(3, MemberRole.Consumer, new HourlyEnergy(H(0), 0m, 6m), new HourlyEnergy(H(1), 0m, 4m)),
        });

        var result = AllToProducers().Calculate(input);
        Assert.Equal(result.TotalIncentiveEur, result.Members.Sum(m => m.IncentiveEur));
    }
}
