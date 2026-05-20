using UnityEngine;

/// <summary>
/// ScriptableObject that defines a single charm: its display data and effect.
/// Create via Right Click -> Create -> Charms -> Charm Data.
/// </summary>
[CreateAssetMenu(fileName = "NewCharmData", menuName = "Charms/Charm Data")]
public class CharmData : ScriptableObject
{
    [Header("Display")]
    public string charmName;
    [TextArea] 
    public string description;
    public Sprite icon;

    [Header("Effect")]
    public CharmEffect effect;
}