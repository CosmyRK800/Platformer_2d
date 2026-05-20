using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Attach to a controller GameObject in the MainMenu scene.
/// Wire the two Button references in the Inspector.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [SerializeField] private Button startButton;
    [SerializeField] private Button quitButton;

    [Header("Hover Arrows")]
    [Tooltip("Left arrow images, one per menu item, in the same order as itemIndex on each MainMenuHoverItem.")]
    [SerializeField] private Image[] leftArrows;

    [Tooltip("Right arrow images, one per menu item, in the same order as itemIndex on each MainMenuHoverItem.")]
    [SerializeField] private Image[] rightArrows;

    private void Start()
    {
        startButton.onClick.AddListener(OnStart);
        quitButton.onClick.AddListener(OnQuit);
        HideAllArrows();
    }

    private void OnStart() => SceneManager.LoadScene(1);

    private void OnQuit() => Application.Quit();

    public void OpenOptions()      => SceneManager.LoadScene(3);
    public void OpenAchievements() => SceneManager.LoadScene(4);
    public void OpenExtras()       => SceneManager.LoadScene(5);

    public void OnItemPointerEnter(int index)
    {
        SetArrows(index, true);
    }

    public void OnItemPointerExit(int index)
    {
        SetArrows(index, false);
    }

    private void SetArrows(int index, bool visible)
    {
        if (index < leftArrows.Length && leftArrows[index] != null)
            leftArrows[index].enabled = visible;
        if (index < rightArrows.Length && rightArrows[index] != null)
            rightArrows[index].enabled = visible;
    }

    private void HideAllArrows()
    {
        for (int i = 0; i < leftArrows.Length; i++)
            if (leftArrows[i] != null) leftArrows[i].enabled = false;
        for (int i = 0; i < rightArrows.Length; i++)
            if (rightArrows[i] != null) rightArrows[i].enabled = false;
    }
}
