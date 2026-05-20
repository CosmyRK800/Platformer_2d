using UnityEngine;

[CreateAssetMenu(fileName = "NewAchievement", menuName = "Achievements/Achievement Data")]
public class AchievementData : ScriptableObject
{
    public string achievementID;
    public string displayName;
    [TextArea]
    public string description;
    public Sprite icon;
}
