using UnityEngine;
using UnityEngine.SceneManagement;

public class ExtrasController : MonoBehaviour
{
    public void GoBack()
    {
        SceneManager.LoadScene(0); // MainMenu
    }
}