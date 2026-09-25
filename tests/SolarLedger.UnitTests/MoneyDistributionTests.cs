using SolarLedger.Application.Settlement;
using Xunit;

namespace SolarLedger.UnitTests;

public class MoneyDistributionTests
{
    [Fact]
    public void Splits_sum_back_to_total_to_the_cent()
    {
        // 100 / 3 does not divide into clean cents — the split must still reconcile.
        var parts = MoneyDistribution.Distribute(100m, new[] { 1m, 1m, 1m });
        Assert.Equal(100m, parts.Sum());
    }

    [Fact]
    public void Leftover_cents_go_to_largest_remainders()
    {
        var parts = MoneyDistribution.Distribute(100m, new[] { 1m, 1m, 1m });
        // 33.34 + 33.33 + 33.33, leftover cent to the first (equal remainders, lower index).
        Assert.Equal(33.34m, parts[0]);
        Assert.Equal(33.33m, parts[1]);
        Assert.Equal(33.33m, parts[2]);
    }

    [Fact]
    public void Proportional_to_weights()
    {
        var parts = MoneyDistribution.Distribute(90m, new[] { 2m, 1m });
        Assert.Equal(60m, parts[0]);
        Assert.Equal(30m, parts[1]);
        Assert.Equal(90m, parts.Sum());
    }

    [Fact]
    public void Zero_weights_yield_zero_and_never_throw()
    {
        var parts = MoneyDistribution.Distribute(50m, new[] { 0m, 0m });
        Assert.Equal(new[] { 0m, 0m }, parts);
    }

    [Fact]
    public void Zero_total_yields_zero()
    {
        var parts = MoneyDistribution.Distribute(0m, new[] { 3m, 7m });
        Assert.Equal(new[] { 0m, 0m }, parts);
    }
}
