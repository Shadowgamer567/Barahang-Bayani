using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class TutorialSceneController : MonoBehaviour
{
    public GameObject[] tutorialPanels;

    public GameObject tutorialOverlay;
    public GameObject tutorialSpotlight;
    public Button tutorialClickZoneButton;

    [Header("Tutorial Configuration")]
    public TutorialStage introductionStage = TutorialStage.MapIntroduction;
    public TutorialStage nextStage = TutorialStage.SelectTutorialCampaign;

    [Header("Panel-Specific Spotlights")]
    public GameObject[] panelSpotlights;


    private int currentPanelIndex = 0;
    private bool showingTutorial = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        tutorialClickZoneButton.onClick.AddListener(NextPanel);

        // Hide every tutorial panel by default.
        foreach (GameObject panel in tutorialPanels)
        {
            panel.SetActive(false);
        }

        tutorialOverlay.SetActive(false);
        if (tutorialSpotlight != null)
        {
            tutorialSpotlight.SetActive(false);
        }

        if (panelSpotlights != null)
        {
            foreach (GameObject spotlight in panelSpotlights)
            {
                if (spotlight != null)
                    spotlight.SetActive(false);
            }
        }

        tutorialClickZoneButton.gameObject.SetActive(false);

        if (TutorialManager.Instance == null)
        {
            return;
        }

        Debug.Log(
        $"[{gameObject.scene.name}] " +
        $"Tutorial Active: {TutorialManager.Instance.tutorialActive}, " +
        $"Current Stage: {TutorialManager.Instance.currentStage}, " +
        $"Expected Stage: {introductionStage}"
);

        if (TutorialManager.Instance.tutorialActive && TutorialManager.Instance.currentStage == introductionStage)
        {
            Debug.Log($"TutorialSceneController ACTIVATED: {gameObject.name} | Stage: {introductionStage}");

            showingTutorial = true;
            currentPanelIndex = 0;

            tutorialClickZoneButton.gameObject.SetActive(true);

            ShowCurrentPanel();
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void ShowCurrentPanel()
    {
        tutorialPanels[currentPanelIndex].SetActive(true);

        UpdateSpotlight();

        if (currentPanelIndex == tutorialPanels.Length - 1)
        {
            // FinalTutorial must wait for the player to click
            // through the last panel before completing.
            if (introductionStage == TutorialStage.FinalExplanation)
            {
                Debug.Log("Final Tutorial: Last panel displayed. Waiting for final click.");
                return;
            }

            // Preserve the existing behavior of other tutorials.
            showingTutorial = false;

            tutorialClickZoneButton.gameObject.SetActive(false);

            TutorialManager.Instance.AdvanceStage(nextStage);

            Debug.Log("Tutorial: Select the Tutorial Campaign.");
        }
    }

    void NextPanel()
    {
        if (!showingTutorial)
        {
            return;
        }

        tutorialPanels[currentPanelIndex].SetActive(false);

        currentPanelIndex++;

        if (currentPanelIndex < tutorialPanels.Length)
        {
            ShowCurrentPanel();
        }

        else if (introductionStage == TutorialStage.FinalExplanation)
        {
            FinishFinalTutorial();
        }
    }

    void FinishFinalTutorial()
    {
        showingTutorial = false;

        tutorialOverlay.SetActive(false);
        tutorialClickZoneButton.gameObject.SetActive(false);

        // Mark the tutorial as completed.
        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.CompleteTutorial();
        }

        // Save completion to the current account's progress.
        if (ProgressManager.Instance != null)
        {
            ProgressManager.Instance.tutorialCompleted = true;
            ProgressManager.Instance.SaveProgress();

            Debug.Log("Final Tutorial: Completion saved to progress.");
        }
        else
        {
            Debug.LogError("Final Tutorial: ProgressManager not found!");
        }

        Debug.Log("Final Tutorial: All tutorial sections completed.");
    }

    void OnDestroy()
    {
        if (tutorialClickZoneButton != null)
        {
            tutorialClickZoneButton.onClick.RemoveListener(NextPanel);
        }
    }

    void UpdateSpotlight()
    {
        if (tutorialSpotlight != null)
        {
            tutorialSpotlight.SetActive(false);
        }

        if (panelSpotlights != null)
        {
            foreach(GameObject spotlight in panelSpotlights)
            {
                if(spotlight != null)
                {
                    spotlight.SetActive(false);
                }
            }
        }

        if (panelSpotlights != null &&
        currentPanelIndex < panelSpotlights.Length &&
        panelSpotlights[currentPanelIndex] != null)
        {
            tutorialOverlay.SetActive(false);
            panelSpotlights[currentPanelIndex].SetActive(true);
        }
       

        else if (tutorialSpotlight != null && currentPanelIndex >= 2)
        {
            tutorialOverlay.SetActive(false);
            tutorialSpotlight.SetActive(true);
        }
        else
        {
            tutorialOverlay.SetActive(true);
        }
    }
}
