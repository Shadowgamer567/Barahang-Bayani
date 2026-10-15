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

        StartNewGame();
    }

    public static void StartNewGame()
    {
        ProgressManager.Instance.NewGame();

        TutorialManager.Instance.StartTutorial();

        SceneTransitionManager.Instance.SwitchScene("MapMenu");
    }
    public async void LoadGame()
    {
        bool progressLoaded =
            await ProgressManager.Instance.LoadProgressForActiveAccount();

        if (!progressLoaded)
        {
            return;
        }

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

    public void SelectTutorial()
    {
        if (TutorialManager.Instance != null &&
        TutorialManager.Instance.tutorialActive &&
        TutorialManager.Instance.currentStage ==
            TutorialStage.SelectTutorialCampaign)
        {
            TutorialManager.Instance.AdvanceStage(TutorialStage.LevelSelectIntroduction);
        }

        SceneTransitionManager.Instance.SwitchScene("LevelSelect_Tutorial");
    }
}
