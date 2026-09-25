using SolarLedger.Domain.Common;
using SolarLedger.Domain.Members;

namespace SolarLedger.Domain.Communities;

/// <summary>
/// A Renewable Energy Community (CER): the configuration under a single primary
/// substation that shared-energy settlement is computed for.
/// Phase 0 introduces only this entity so the first migration creates a real table
/// and proves the EF Core ↔ Oracle round-trip. The rest of the model (Member, Pod,
/// EnergyReading, SettlementRun, MemberSettlement) arrives in Phase 1.
/// </summary>
public class Community : Entity
{
    public required string Name { get; set; }

    /// <summary>Codice cabina primaria — members must sit under the same one.</summary>
    public required string PrimarySubstationCode { get; set; }

    /// <summary>Incentive tariff in €/MWh applied to shared energy. Simplified to a
    /// single configurable value (the real GSE tariff is a formula).</summary>
    public decimal IncentiveTariffEurPerMwh { get; set; }

    /// <summary>How the incentive is split among members. Free-form for now; becomes a
    /// proper strategy in Phase 2.</summary>
    public string DistributionPolicy { get; set; } = "AllToProducers";

    public DateTimeOffset CreatedOnUtc { get; set; } = DateTimeOffset.UtcNow;

    public List<Member> Members { get; } = new();
}
