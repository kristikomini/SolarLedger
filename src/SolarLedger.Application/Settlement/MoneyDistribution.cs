namespace SolarLedger.Application.Settlement;

/// <summary>
/// Splits a euro amount across weighted recipients so the parts sum back to the total
/// exactly, to the cent. Uses the largest-remainder method: floor every share to cents,
/// then hand the leftover cents to the largest fractional remainders. This is the kind
/// of thing that quietly loses money if done with naive rounding — so it is tested.
/// </summary>
public static class MoneyDistribution
{
    public static decimal[] Distribute(decimal totalEur, IReadOnlyList<decimal> weights)
    {
        var n = weights.Count;
        var result = new decimal[n];

        var totalWeight = 0m;
        for (var i = 0; i < n; i++) totalWeight += weights[i];

        if (n == 0 || totalWeight <= 0m || totalEur <= 0m)
            return result;

        // Work in integer cents so the reconciliation is exact.
        var totalCents = (long)Math.Round(totalEur * 100m, MidpointRounding.AwayFromZero);

        var floorCents = new long[n];
        var remainders = new decimal[n];
        var allocated = 0L;

        for (var i = 0; i < n; i++)
        {
            var exact = totalCents * weights[i] / totalWeight;
            var floor = (long)Math.Floor(exact);
            floorCents[i] = floor;
            remainders[i] = exact - floor;
            allocated += floor;
        }

        var leftover = totalCents - allocated;

        // Give one extra cent to the largest remainders (ties: lower index first).
        var order = Enumerable.Range(0, n)
            .OrderByDescending(i => remainders[i])
            .ThenBy(i => i)
            .ToList();

        for (var k = 0; k < leftover; k++)
            floorCents[order[k % n]] += 1;

        for (var i = 0; i < n; i++)
            result[i] = floorCents[i] / 100m;

        return result;
    }
}
