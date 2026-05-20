/// <summary>
/// Lightweight data bag returned by GameManager.GetSlotPreview().
/// Not a MonoBehaviour — no Unity dependencies.
/// </summary>
public class SlotPreview
{
    public bool   HasData;
    public string LastCheckpointName;
    public float  PlayTimeSeconds;
}
