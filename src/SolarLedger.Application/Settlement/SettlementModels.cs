using SolarLedger.Domain.Members;

namespace SolarLedger.Application.Settlement;

/// <summary>One hour of a single member's metered energy.</summary>
public record HourlyEnergy(DateTime Hour, decimal InjectedKwh, decimal WithdrawnKwh);

/// <summary>A member and its hourly energy over the settlement period.</summary>
public record MemberEnergy(long MemberId, MemberRole Role, IReadOnlyList<HourlyEnergy> Hours);

/// <summary>Everything the engine needs, decoupled from EF and the database.</summary>
public record SettlementInput(decimal TariffEurPerMwh, IReadOnlyList<MemberEnergy> Members);

/// <summary>Community-level shared energy for one hour (audit line).</summary>
public record HourlyShareLine(
    DateTime Hour, decimal InjectedKwh, decimal WithdrawnKwh, decimal SharedKwh);

/// <summary>What one member is attributed and paid.</summary>
public record MemberAllocation(long MemberId, decimal AttributedKwh, decimal IncentiveEur);

/// <summary>Per-member energy that counted toward shared hours — the basis for splitting.</summary>
public record MemberSharedTotals(
    long MemberId,
    MemberRole Role,
    decimal InjectedInSharedHoursKwh,
    decimal WithdrawnInSharedHoursKwh);

/// <summary>Input handed to a distribution policy once the engine knows the pot.</summary>
public record SettlementContext(
    decimal TotalIncentiveEur, IReadOnlyList<MemberSharedTotals> Members);

/// <summary>The full result of a settlement calculation.</summary>
public record SettlementResult(
    decimal TotalSharedKwh,
    decimal TotalIncentiveEur,
    string PolicyName,
    IReadOnlyList<HourlyShareLine> Hourly,
    IReadOnlyList<MemberAllocation> Members);
