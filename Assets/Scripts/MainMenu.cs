using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void PlayGame()
    {
        SceneManager.LoadScene("MapMenu");
    }

    public void Options()
    {
        Debug.Log("Options not implemented yet");
    }
    public void QuitGame()
    {
        Application.Quit();
    }
    public void RegionSelect_Back()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
