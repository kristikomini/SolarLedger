using SolarLedger.Application.Metering;
using SolarLedger.Domain.Pods;
using Xunit;

namespace SolarLedger.UnitTests;

public class ReadingGeneratorTests
{
    private static readonly DateTime Start = new(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);

    private static Pod Pod(PodType type, long id = 1) =>
        new() { Id = id, PodCode = $"IT{id:0000000000}", Type = type };

    [Fact]
    public void Generates_one_reading_per_hour()
    {
        var readings = ReadingGenerator.Generate(Pod(PodType.Production), Start, days: 3, seed: 1);
        Assert.Equal(3 * 24, readings.Count);
    }

    [Fact]
    public void Production_pod_only_injects_never_withdraws()
    {
        var readings = ReadingGenerator.Generate(Pod(PodType.Production), Start, days: 2, seed: 1);
        Assert.All(readings, r => Assert.Equal(0m, r.WithdrawnKwh));
        Assert.Contains(readings, r => r.InjectedKwh > 0m);
    }

    [Fact]
    public void Consumption_pod_only_withdraws_never_injects()
    {
        var readings = ReadingGenerator.Generate(Pod(PodType.Consumption), Start, days: 2, seed: 1);
        Assert.All(readings, r => Assert.Equal(0m, r.InjectedKwh));
        Assert.Contains(readings, r => r.WithdrawnKwh > 0m);
    }

    [Fact]
    public void Solar_is_zero_at_night_and_positive_at_midday()
    {
        var readings = ReadingGenerator.Generate(Pod(PodType.Production), Start, days: 1, seed: 1);
        var midnight = readings.Single(r => r.Hour.Hour == 0);
        var midday = readings.Single(r => r.Hour.Hour == 13);

        Assert.Equal(0m, midnight.InjectedKwh);
        Assert.True(midday.InjectedKwh > 0m);
    }

    [Fact]
    public void Is_deterministic_for_a_given_seed()
    {
        var a = ReadingGenerator.Generate(Pod(PodType.Both), Start, days: 2, seed: 7);
        var b = ReadingGenerator.Generate(Pod(PodType.Both), Start, days: 2, seed: 7);

        Assert.Equal(a.Count, b.Count);
        for (var i = 0; i < a.Count; i++)
        {
            Assert.Equal(a[i].InjectedKwh, b[i].InjectedKwh);
            Assert.Equal(a[i].WithdrawnKwh, b[i].WithdrawnKwh);
        }
    }
}
