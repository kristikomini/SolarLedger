namespace SolarLedger.Domain.Members;

/// <summary>Role of a member within the community's energy balance.</summary>
public enum MemberRole
{
    /// <summary>Only produces (e.g. a shared PV plant).</summary>
    Producer,

    /// <summary>Only consumes.</summary>
    Consumer,

    /// <summary>Both produces and consumes (has PV behind its own meter).</summary>
    Prosumer
}
