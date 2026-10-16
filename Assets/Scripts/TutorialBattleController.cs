using UnityEngine;
using UnityEngine.UI;

public class TutorialBattleController : MonoBehaviour
{
    [Header("Battle Reference")]
    public BattleControl battleControl;

    [Header("UI Main Reference")]
    public UIMain uiMain;

    [Header("Tutorial UI")]
    public GameObject[] tutorialPanels;
    public GameObject tutorialOverlay;
    public Button tutorialClickZoneButton;

    [Header("Panel Specific Spotlights")]
    public GameObject[] panelSpotlights;

    private int currentPanelIndex = 0;
    private bool showingTutorial = false;
    private bool waitingForCardSelection = false;
    private bool showingFullCardTutorial = false;
    private bool waitingForCardToBePlayed = false;
    private bool waitingForCardResolution = false;
    private bool quizTutorialCompleted = false;
    private bool waitingForQuizCard = false;

    private void Start()
    {
        // Hide tutorial UI by default.
        foreach (GameObject panel in tutorialPanels)
        {
            panel.SetActive(false);
        }

        tutorialOverlay.SetActive(false);
        tutorialClickZoneButton.gameObject.SetActive(false);

        foreach (GameObject spotlight in panelSpotlights)
        {
            if (spotlight != null)
                spotlight.SetActive(false);
        }

        // Connect the click zone.
        tutorialClickZoneButton.onClick.AddListener(NextPanel);

        // Listen for the first battle starting.
        if (battleControl != null)
        {
            battleControl.OnFirstBattleStarted += OnFirstBattleStarted;

            battleControl.OnCardPlayAccepted += OnCardPlayAccepted;
            battleControl.OnCardResolutionComplete += OnCardResolutionComplete;
        }

        if (uiMain != null)
        {
            uiMain.OnFullCardOpened += OnFullCardOpened;
        }
    }

    private void OnFirstBattleStarted()
    {
        if (TutorialManager.Instance == null ||
            !TutorialManager.Instance.tutorialActive ||
            TutorialManager.Instance.currentStage !=
                TutorialStage.BattleTutorial)
        {
            return;
        }

        Debug.Log("Battle Tutorial Started");

        showingTutorial = true;
        currentPanelIndex = 0;

        // Pause gameplay after the walking sequence.
        Time.timeScale = 0f;

        tutorialOverlay.SetActive(true);
        tutorialClickZoneButton.gameObject.SetActive(true);

        ShowCurrentPanel();
    }

    private void ShowCurrentPanel()
    {
        tutorialPanels[currentPanelIndex].SetActive(true);

        UpdateSpotlight();

        // Panel 12: Wait for the player to select a card.
        if (currentPanelIndex == 12)
        {
            showingTutorial = false;
            waitingForCardSelection = true;

            tutorialClickZoneButton.gameObject.SetActive(false);

            Time.timeScale = 1f;

            Debug.Log("Battle Tutorial: Waiting for card selection.");
        }

        // Panel 14: Allow the player to interact with the Full Card.
        else if (currentPanelIndex == 15)
        {
            showingTutorial = false;
            showingFullCardTutorial = false;
            waitingForCardToBePlayed = true;

            tutorialClickZoneButton.gameObject.SetActive(false);

            Time.timeScale = 1f;

            Debug.Log("Battle Tutorial: Waiting for card to be played.");
        }
    }

    private void NextPanel()
    {
        if (!showingTutorial)
            return;

        // Panel 17 is the final panel of the card explanation.
        if (currentPanelIndex == 17)
        {
            FinishCardExplanation();
            return;
        }

        tutorialPanels[currentPanelIndex].SetActive(false);

        currentPanelIndex++;

        if (currentPanelIndex < tutorialPanels.Length)
        {
            ShowCurrentPanel();
        }
    }

    private void OnFullCardOpened()
    {
        if (!waitingForCardSelection)
            return;

        if (TutorialManager.Instance == null ||
            !TutorialManager.Instance.tutorialActive ||
            TutorialManager.Instance.currentStage != TutorialStage.BattleTutorial)
            return;

        waitingForCardSelection = false;
        showingFullCardTutorial = true;
        showingTutorial = true;

        // Hide Panel 12.
        tutorialPanels[currentPanelIndex].SetActive(false);

        // Begin Full Card explanation at Panel 13.
        currentPanelIndex = 13;

        // Pause gameplay.
        Time.timeScale = 0f;

        // Enable the tutorial click zone.
        tutorialClickZoneButton.gameObject.SetActive(true);

        ShowCurrentPanel();

        Debug.Log("Battle Tutorial: Full Card explanation started.");
    }

    private void OnCardPlayAccepted()
    {
        if (!waitingForCardToBePlayed)
            return;

        waitingForCardToBePlayed = false;
        waitingForCardResolution = true;

        // Hide Panel 15.
        tutorialPanels[currentPanelIndex].SetActive(false);

        // Hide all spotlights.
        foreach (GameObject spotlight in panelSpotlights)
        {
            if (spotlight != null)
                spotlight.SetActive(false);
        }

        tutorialOverlay.SetActive(false);
        tutorialClickZoneButton.gameObject.SetActive(false);

        Debug.Log("Battle Tutorial: Card accepted, waiting for resolution.");
    }

    private void OnCardResolutionComplete()
    {
        if (!waitingForCardResolution)
            return;

        waitingForCardResolution = false;

        // Avoid interrupting the level victory sequence.
        if (battleControl.isLevelFinished)
            return;

        Debug.Log("Battle Tutorial: Card resolved.");

        // Pause before showing the next explanation.
        Time.timeScale = 0f;

        showingTutorial = true;
        currentPanelIndex = 16;

        tutorialClickZoneButton.gameObject.SetActive(true);

        ShowCurrentPanel();
    }

    private void OnDestroy()
    {
        if (battleControl != null)
        {
            battleControl.OnFirstBattleStarted += OnFirstBattleStarted;

            battleControl.OnCardPlayAccepted -= OnCardPlayAccepted;
            battleControl.OnCardResolutionComplete -= OnCardResolutionComplete;
        }

        if (tutorialClickZoneButton != null)
        {
            tutorialClickZoneButton.onClick.RemoveListener(NextPanel);
        }

        if (uiMain != null)
        {
            uiMain.OnFullCardOpened -= OnFullCardOpened;
        }

        // Avoid leaving the game paused if this controller is destroyed
        // while its introduction is active.
        if (showingTutorial || showingFullCardTutorial)
        {
            Time.timeScale = 1f;
        }
    }

    private void UpdateSpotlight()
    {
        foreach (GameObject spotlight in panelSpotlights)
        {
            if (spotlight != null)
                spotlight.SetActive(false);
        }

        if (currentPanelIndex < panelSpotlights.Length &&
            panelSpotlights[currentPanelIndex] != null)
        {
            tutorialOverlay.SetActive(false);
            panelSpotlights[currentPanelIndex].SetActive(true);
        }
        else
        {
            tutorialOverlay.SetActive(true);
        }
    }

    private void FinishCardExplanation()
    {
        showingTutorial = false;

        tutorialPanels[currentPanelIndex].SetActive(false);

        tutorialOverlay.SetActive(false);
        tutorialClickZoneButton.gameObject.SetActive(false);

        foreach (GameObject spotlight in panelSpotlights)
        {
            if (spotlight != null)
                spotlight.SetActive(false);
        }

        waitingForQuizCard = !quizTutorialCompleted;

        Time.timeScale = 1f;

        Debug.Log("Battle Tutorial: Card explanation finished. Waiting for Quiz Card.");
    }
}