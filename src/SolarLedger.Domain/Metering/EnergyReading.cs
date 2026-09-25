using SolarLedger.Domain.Common;
using SolarLedger.Domain.Pods;

namespace SolarLedger.Domain.Metering;

/// <summary>
/// One hour of metered energy for a POD: how much it fed into the grid (injected) and
/// how much it drew from it (withdrawn). The settlement's shared energy per hour is
/// min(Σ injected, Σ withdrawn) across the community — so both directions matter.
/// </summary>
public class EnergyReading : Entity
{
    public long PodId { get; set; }
    public Pod? Pod { get; set; }

    /// <summary>Start of the hourly interval, in UTC, truncated to the hour.</summary>
    public DateTime Hour { get; set; }

    public decimal InjectedKwh { get; set; }

    public decimal WithdrawnKwh { get; set; }
}
