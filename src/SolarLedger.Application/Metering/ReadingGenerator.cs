using SolarLedger.Domain.Metering;
using SolarLedger.Domain.Pods;

namespace SolarLedger.Application.Metering;

/// <summary>
/// Generates realistic-ish hourly meter readings for a POD: a solar bell curve for
/// production, morning/evening peaks for consumption. Pure and deterministic given a
/// seed, so it can be unit tested and reproduced. Not a physical model — enough shape
/// that the settlement (shared energy = min of injection and withdrawal per hour)
/// produces interesting, non-trivial results.
/// </summary>
public static class ReadingGenerator
{
    /// <param name="pod">POD to generate for (its <see cref="Pod.Type"/> shapes the curve).</param>
    /// <param name="startDateUtc">First day (inclusive), treated as UTC midnight.</param>
    /// <param name="days">Number of consecutive days.</param>
    /// <param name="seed">Deterministic jitter seed.</param>
    public static IReadOnlyList<EnergyReading> Generate(
        Pod pod, DateTime startDateUtc, int days, int seed)
    {
        if (days <= 0) throw new ArgumentOutOfRangeException(nameof(days));

        var start = DateTime.SpecifyKind(startDateUtc.Date, DateTimeKind.Utc);
        // Seed folds in the POD id so different PODs get different-but-reproducible noise.
        var rng = new Random(seed ^ (int)(pod.Id & 0x7fffffff));
        var readings = new List<EnergyReading>(days * 24);

        for (var d = 0; d < days; d++)
        {
            for (var h = 0; h < 24; h++)
            {
                var hour = start.AddDays(d).AddHours(h);
                var jitter = 0.85 + rng.NextDouble() * 0.30; // 0.85 .. 1.15

                var solar = SolarKw(h) * jitter;      // kWh produced this hour
                var load = ConsumptionKw(h) * jitter; // kWh consumed this hour

                var (injected, withdrawn) = pod.Type switch
                {
                    // A pure plant: ~10 kW peak fed to grid, negligible draw.
                    PodType.Production => (solar * 10.0, 0.0),
                    // A household: only draws.
                    PodType.Consumption => (0.0, load * 1.5),
                    // Prosumer: ~3 kW panel behind the meter; net after self-use.
                    PodType.Both => NetProsumer(solar * 3.0, load * 1.5),
                    _ => (0.0, 0.0)
                };

                readings.Add(new EnergyReading
                {
                    PodId = pod.Id,
                    Hour = hour,
                    InjectedKwh = Round(injected),
                    WithdrawnKwh = Round(withdrawn)
                });
            }
        }

        return readings;
    }

    private static (double injected, double withdrawn) NetProsumer(double solar, double load)
    {
        var net = solar - load;
        return net >= 0 ? (net, 0.0) : (0.0, -net);
    }

    /// <summary>Normalised solar output 0..1: a bell peaking at ~13:00, zero at night.</summary>
    private static double SolarKw(int hour)
    {
        const int sunrise = 6, sunset = 20;
        if (hour < sunrise || hour > sunset) return 0.0;
        var t = (hour - sunrise) / (double)(sunset - sunrise); // 0..1
        return Math.Sin(Math.PI * t); // 0 at edges, 1 at midday
    }

    /// <summary>Normalised consumption 0..~1: base load plus morning and evening peaks.</summary>
    private static double ConsumptionKw(int hour)
    {
        var baseLoad = 0.25;
        var morning = Math.Exp(-Math.Pow(hour - 8, 2) / 3.0);   // peak ~08:00
        var evening = Math.Exp(-Math.Pow(hour - 20, 2) / 4.0);  // peak ~20:00
        return baseLoad + 0.6 * morning + 0.8 * evening;
    }

    private static decimal Round(double kwh) => Math.Round((decimal)kwh, 3);
}
