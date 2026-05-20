using UnityEngine;
using UnityEngine.SceneManagement;

public class OptionsController : MonoBehaviour
{
    public void GoBack()
    {
        SceneManager.LoadScene(0); // MainMenu
    }
}