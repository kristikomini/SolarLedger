using SolarLedger.Domain.Common;
using SolarLedger.Domain.Communities;
using SolarLedger.Domain.Pods;

namespace SolarLedger.Domain.Members;

/// <summary>A participant in a community. Owns one or more delivery points (POD).</summary>
public class Member : Entity
{
    public long CommunityId { get; set; }
    public Community? Community { get; set; }

    public required string Name { get; set; }

    public MemberRole Role { get; set; }

    /// <summary>Where the member's incentive share is paid: IBAN, or a bill reference.</summary>
    public required string PaymentReference { get; set; }

    public List<Pod> Pods { get; } = new();
}
