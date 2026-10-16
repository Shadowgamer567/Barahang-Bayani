using UnityEngine;
using UnityEngine.UI;

public class TutorialBattleController : MonoBehaviour
{
    [Header("Battle Reference")]
    public BattleControl battleControl;

    [Header("Tutorial UI")]
    public GameObject[] tutorialPanels;
    public GameObject tutorialOverlay;
    public Button tutorialClickZoneButton;

    [Header("Panel Specific Spotlights")]
    public GameObject[] panelSpotlights;

    private int currentPanelIndex = 0;
    private bool showingTutorial = false;

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

        if (currentPanelIndex == tutorialPanels.Length - 1)
        {
            showingTutorial = false;

            tutorialClickZoneButton.gameObject.SetActive(false);

            Time.timeScale = 1f;

            Debug.Log("Battle Tutorial: Waiting for card selection.");
        }
    }

    private void NextPanel()
    {
        if (!showingTutorial)
            return;

        tutorialPanels[currentPanelIndex].SetActive(false);

        currentPanelIndex++;

        if (currentPanelIndex < tutorialPanels.Length)
        {
            ShowCurrentPanel();
        }
    }

    private void OnDestroy()
    {
        if (battleControl != null)
        {
            battleControl.OnFirstBattleStarted -= OnFirstBattleStarted;
        }

        if (tutorialClickZoneButton != null)
        {
            tutorialClickZoneButton.onClick.RemoveListener(NextPanel);
        }

        // Avoid leaving the game paused if this controller is destroyed
        // while its introduction is active.
        if (showingTutorial)
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
}