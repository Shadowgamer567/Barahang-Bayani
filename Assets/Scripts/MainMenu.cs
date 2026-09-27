using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public ProgressManager loadGame;
    public void PlayGame()
    {
        if (CurrentAccount.ActiveAccount == null)
        {
            Debug.LogError("Select or Create an Account");

            AccountManager.Instance.PromptForAccountThenStartGame();
            return;
        }

        ProgressManager.Instance.NewGame();

        SceneTransitionManager.Instance.SwitchScene("MapMenu");
    }
    public void LoadGame()
    {
        ProgressManager.Instance.LoadProgress();

        string sceneToLoad = ProgressManager.Instance.lastCampaignScene;

        Debug.Log("Loading last Campaign" + sceneToLoad);

        SceneTransitionManager.Instance.SwitchScene(sceneToLoad);
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
        SceneTransitionManager.Instance.SwitchScene("MainMenu");
    }

    public void SelectRegion1()
    {
        SceneTransitionManager.Instance.SwitchScene("LevelSelect_Rizal");
    }
}
