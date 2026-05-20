using UnityEngine;

/// <summary>
/// Abstract base for all charm effects.
/// Subclass this ScriptableObject to create new charm types.
/// </summary>
public abstract class CharmEffect : ScriptableObject
{
    public abstract void Apply(PlayerHealth health);
    public abstract void Remove(PlayerHealth health);

    /// <summary>How many extra lives this effect adds to max/current health while equipped.</summary>
    public virtual int HealthBonus => 0;
}