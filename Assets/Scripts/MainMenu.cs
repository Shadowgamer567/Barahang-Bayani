using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public ProgressManager loadGame;
    public void PlayGame()
    {
        ProgressManager.Instance.NewGame();
        SceneManager.LoadScene("MapMenu");
    }
    public void LoadGame()
    {
        ProgressManager.Instance.LoadProgress();

        string sceneToLoad = ProgressManager.Instance.lastCampaignScene;

        Debug.Log("Loading last Campaign" + sceneToLoad);

        SceneManager.LoadScene(sceneToLoad);
    }

    public void Options()
    {
        Debug.Log("This has not been implemented yet");
    }
    public void QuitGame()
    {
        Application.Quit();
    }

    public void RegionSelect_Back()
    {
        SceneManager.LoadScene("MainMenu");
    }

    public void SelectRegion1()
    {
        SceneManager.LoadScene("LevelSelect_Rizal");
    }
}
