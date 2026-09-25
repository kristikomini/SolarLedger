namespace SolarLedger.Domain.Common;

/// <summary>Base class for entities with a surrogate long identity.</summary>
public abstract class Entity
{
    public long Id { get; set; }
}
