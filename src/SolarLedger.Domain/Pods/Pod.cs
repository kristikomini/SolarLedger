using SolarLedger.Domain.Common;
using SolarLedger.Domain.Members;
using SolarLedger.Domain.Metering;

namespace SolarLedger.Domain.Pods;

/// <summary>
/// Point of Delivery — a physical connection point on the grid, identified by its POD
/// code. Meter readings are attached here; settlement aggregates across the community's
/// PODs.
/// </summary>
public class Pod : Entity
{
    public long MemberId { get; set; }
    public Member? Member { get; set; }

    /// <summary>The POD code (e.g. IT001E00000001). Natural identifier from the meter.</summary>
    public required string PodCode { get; set; }

    public PodType Type { get; set; }

    public List<EnergyReading> Readings { get; } = new();
}
