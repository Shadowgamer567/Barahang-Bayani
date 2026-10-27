using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    public static bool IsOptionsMenuOpen { get; private set; }

    public ProgressManager loadGame;

    [SerializeField] private GameObject optionsMenu;
    [SerializeField] private Image optionsBackdrop;
    [SerializeField] private GameObject accountsButton;
    [SerializeField] private GameObject optionsBackButton;

    private bool accountsButtonWasActive;

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
        if (optionsMenu != null && optionsMenu.activeSelf)
        {
            return;
        }

        if (optionsMenu == null)
        {
            Debug.LogError("Options menu is not assigned.", this);
            return;
        }

        PanelHider panelHider = optionsMenu.GetComponent<PanelHider>();
        if (panelHider == null)
        {
            Debug.LogError("Options menu requires a PanelHider component.", optionsMenu);
            return;
        }

        if (accountsButton == null)
        {
            Debug.LogError("Accounts button is not assigned.", this);
            return;
        }

        if (optionsBackButton == null)
        {
            Debug.LogError("Options back button is not assigned.", this);
            return;
        }

        accountsButtonWasActive = accountsButton.activeSelf;
        accountsButton.SetActive(false);
        optionsBackButton.SetActive(true);

        if (optionsBackdrop != null)
        {
            Color backdropColor = optionsBackdrop.color;
            backdropColor.a = 1f;
            optionsBackdrop.color = backdropColor;
            optionsBackdrop.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogError("Options backdrop is not assigned.", this);
        }

        optionsMenu.SetActive(true);
        IsOptionsMenuOpen = true;
    }

    public void BackFromOptions()
    {
        if (optionsMenu == null)
        {
            Debug.LogError("Options menu is not assigned.", this);
            return;
        }

        PanelHider panelHider = optionsMenu.GetComponent<PanelHider>();
        if (panelHider == null)
        {
            Debug.LogError("Options menu requires a PanelHider component.", optionsMenu);
            return;
        }

        panelHider.RestorePanels();
        optionsMenu.SetActive(false);

        if (optionsBackdrop != null)
        {
            optionsBackdrop.gameObject.SetActive(false);
        }

        if (accountsButton != null)
        {
            accountsButton.SetActive(accountsButtonWasActive);
        }

        if (optionsBackButton != null)
        {
            optionsBackButton.SetActive(false);
        }

        IsOptionsMenuOpen = false;
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
        // Prevent selecting Rizal Campaign while the tutorial
        // requires the player to select Tutorial Campaign.
        if (TutorialManager.Instance != null && TutorialManager.Instance.tutorialActive)
        {
            Debug.Log("Rizal Campaign blocked during tutorial.");
            return;
        }

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
