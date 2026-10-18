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
        // Check if an account is currently selected.
        if (CurrentAccount.ActiveAccount == null)
        {
            Debug.LogError("Select or Create an Account");

            AccountManager.Instance.PromptForAccountThenLoadGame();
            return;
        }

        bool progressLoaded =
            await ProgressManager.Instance.LoadProgressForActiveAccount();

        if (!progressLoaded)
        {
            return;
        }

        // Synchronize tutorial state with the loaded account's progress.
        if (TutorialManager.Instance != null)
        {
            if (ProgressManager.Instance.tutorialCompleted)
            {
                TutorialManager.Instance.CompleteTutorial();

                Debug.Log("MainMenu: Tutorial already completed. Skipping tutorial.");
            }
            else
            {
                TutorialManager.Instance.StartTutorial();

                Debug.Log("MainMenu: Tutorial incomplete. Restarting tutorial.");

                SceneTransitionManager.Instance.SwitchScene("MapMenu");
                return;
            }
        }

        string sceneToLoad = ProgressManager.Instance.lastCampaignScene;

        Debug.Log("Loading last Campaign " + sceneToLoad);

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
