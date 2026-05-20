using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Attach to the root of the AchievementRow prefab and wire up the three fields.
/// AchievementsScreenController reads these references to populate each row.
/// </summary>
public class AchievementRowEntry : MonoBehaviour
{
    public Image icon;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descText;
}
