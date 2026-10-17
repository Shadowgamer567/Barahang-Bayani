using UnityEngine;
using UnityEngine.UI;

public class TutorialBattleController : MonoBehaviour
{
    [Header("Battle Reference")]
    public BattleControl battleControl;

    [Header("UI Main Reference")]
    public UIMain uiMain;

    [Header("Quiz Reference")]
    public QuizManager quizManager;

    [Header("Card Panel Reference")]
    public CardPanelManager cardPanelManager;

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
    private bool battleIntroductionStarted = false;
    private bool selectingScriptedQuizCard = false;

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
            battleControl.OnSecondBattleStarted += OnSecondBattleStarted;

            battleControl.OnCardPlayAccepted += OnCardPlayAccepted;
            battleControl.OnCardResolutionComplete += OnCardResolutionComplete;
        }

        if (uiMain != null)
        {
            uiMain.OnFullCardOpened += OnFullCardOpened;
        }

        if (quizManager != null)
        {
            quizManager.OnQuizOpened += OnQuizOpened;
            quizManager.OnQuizInfoOpened += OnQuizInfoOpened;
        }
    }

    private void OnFirstBattleStarted()
    {
        if (TutorialManager.Instance == null ||
            !TutorialManager.Instance.tutorialActive ||
            TutorialManager.Instance.currentStage != TutorialStage.BattleTutorial)
        {
            return;
        }

        // Prevent the battle introduction from restarting.
        if (battleIntroductionStarted)
        {
            Debug.Log("Battle Tutorial: Introduction already started. Skipping.");
            return;
        }

        battleIntroductionStarted = true;

        // Guarantee non-Quiz Cards in the first tutorial hand.
        if (cardPanelManager != null)
        {
            cardPanelManager.ForceNonQuizStartingHand();
        }
        Debug.Log("Battle Tutorial Started");

        showingTutorial = true;
        currentPanelIndex = 0;

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

        // Allow the player to select the scripted Quiz Card
        // immediately when Panel 26 appears.
        if (currentPanelIndex == 26 && showingTutorial)
        {
            BeginScriptedQuizCardSelection();
        }
    }

    private void NextPanel()
    {
        if (!showingTutorial)
            return;

        // Finish the regular card explanation.
        if (currentPanelIndex == 17)
        {
            FinishCardExplanation();
            return;
        }

        // Finish the quiz introduction and allow answering.
        if (currentPanelIndex == 21)
        {
            FinishQuizIntroduction();
            return;
        }

        // Finish the quiz information explanation.
        if (currentPanelIndex == 23)
        {
            FinishQuizInfoExplanation();
            return;
        }

        // Finish the scripted Quiz Card introduction.
       /* if (currentPanelIndex == 26)
        {
            BeginScriptedQuizCardSelection();
            return;
        }
        */

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

        // The scripted Quiz Card uses its own selection flow.
        if (selectingScriptedQuizCard)
        {
            selectingScriptedQuizCard = false;
            waitingForCardSelection = false;
            waitingForCardToBePlayed = true;

            // Hide the instruction panel.
            tutorialPanels[currentPanelIndex].SetActive(false);

            // Hide the fifth-card spotlight.
            foreach (GameObject spotlight in panelSpotlights)
            {
                if (spotlight != null)
                    spotlight.SetActive(false);
            }

            tutorialOverlay.SetActive(false);
            tutorialClickZoneButton.gameObject.SetActive(false);

            Debug.Log("Battle Tutorial: Scripted Quiz Card opened. Waiting for it to be played.");

            return;
        }

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
        {
            return;
        }
           
        // The scripted Quiz Card transitions directly into the quiz tutorial.
        if (currentPanelIndex == 26)
        {
            waitingForCardToBePlayed = false;

            Debug.Log("Battle Tutorial: Scripted Quiz Card played. Waiting for quiz.");

            return;
        }

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

    private void OnQuizOpened()
    {
        if (TutorialManager.Instance == null ||
            !TutorialManager.Instance.tutorialActive ||
            TutorialManager.Instance.currentStage != TutorialStage.BattleTutorial)
        {
            return;
        }

        if (quizTutorialCompleted)
            return;

        // Avoid interrupting an earlier tutorial explanation.
        if (showingTutorial)
        {
            Debug.Log("Battle Tutorial: Quiz opened during another tutorial phase.");
            return;
        }

        Debug.Log("Battle Tutorial: Quiz Card detected.");

        waitingForQuizCard = false;
        showingTutorial = true;

        currentPanelIndex = 18;

        // QuizManager already pauses gameplay.
        tutorialClickZoneButton.gameObject.SetActive(true);

        ShowCurrentPanel();

        Debug.Log("Battle Tutorial: Quiz explanation started.");
    }

    private void FinishQuizIntroduction()
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

        // The quiz is still open, so do not resume gameplay here.
        Debug.Log("Battle Tutorial: Waiting for quiz answer.");
    }

    private void BeginScriptedQuizCardSelection()
    {
        Debug.Log("Battle Tutorial: Waiting for scripted Quiz Card selection.");

        selectingScriptedQuizCard = true;

        showingTutorial = false;
        waitingForCardSelection = true;

        // Keep Panel 26 visible as an instruction.
        tutorialClickZoneButton.gameObject.SetActive(false);

        // Allow the player to interact with the card.
        Time.timeScale = 1f;
    }

    private void OnQuizInfoOpened()
    {
        if (TutorialManager.Instance == null ||
            !TutorialManager.Instance.tutorialActive ||
            TutorialManager.Instance.currentStage != TutorialStage.BattleTutorial)
        {
            return;
        }

        if (quizTutorialCompleted)
            return;

        Debug.Log("Battle Tutorial: Quiz information panel detected.");

        // Start the QuizInfoPanel explanation.
        showingTutorial = true;
        currentPanelIndex = 22;

        // Prevent tutorial clicks from closing QuizInfoPanel.
        quizManager.blockInfoPanelDismissal = true;

        // Show the tutorial click zone.
        tutorialClickZoneButton.gameObject.SetActive(true);

        // Display Panel 22 and its spotlight.
        ShowCurrentPanel();

        Debug.Log("Battle Tutorial: Quiz information explanation started.");
    }

    private void OnSecondBattleStarted()
    {
        if (TutorialManager.Instance == null || !TutorialManager.Instance.tutorialActive || TutorialManager.Instance.currentStage != TutorialStage.BattleTutorial)
        {
            return;
        }

        if (quizTutorialCompleted)
        {
            Debug.Log("Battle Tutorial: Quiz tutorial already completed. Skipping fallback.");
            return;
        }

        Debug.Log("Battle Tutorial: Second battle detected. Quiz Card fallback required.");

        if (cardPanelManager == null)
        {
            Debug.LogError("Battle Tutorial: CardPanelManager reference is missing!");
            return;
        }

        // Load the scripted Quiz Card before displaying the tutorial.
        if (!cardPanelManager.ForceQuizCardInFifthSlot())
        {
            return;
        }

        // Pause gameplay for the Quiz Card introduction.
        Time.timeScale = 0f;

        showingTutorial = true;
        currentPanelIndex = 24;

        // Display Panel 24 and its spotlight.
        tutorialOverlay.SetActive(true);
        tutorialClickZoneButton.gameObject.SetActive(true);

        ShowCurrentPanel();

        Debug.Log("Battle Tutorial: Scripted Quiz Card introduction started.");
    }

    private void OnDestroy()
    {
        if (battleControl != null)
        {
            battleControl.OnFirstBattleStarted -= OnFirstBattleStarted;
            battleControl.OnSecondBattleStarted -= OnSecondBattleStarted;

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

        if (quizManager != null)
        {
            quizManager.OnQuizOpened -= OnQuizOpened;
            quizManager.OnQuizInfoOpened -= OnQuizInfoOpened;
            quizManager.blockInfoPanelDismissal = false;
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

    private void FinishQuizInfoExplanation()
    {
        showingTutorial = false;
        quizTutorialCompleted = true;

        tutorialPanels[currentPanelIndex].SetActive(false);

        tutorialOverlay.SetActive(false);
        tutorialClickZoneButton.gameObject.SetActive(false);

        foreach (GameObject spotlight in panelSpotlights)
        {
            if (spotlight != null)
                spotlight.SetActive(false);
        }

        // Leave QuizManager in control of the paused state.
        // The player can now dismiss the QuizInfoPanel normally.

        // Allow the player to dismiss QuizInfoPanel normally.
        quizManager.blockInfoPanelDismissal = false;

        Debug.Log("Battle Tutorial: Quiz information explanation finished.");
    }
}