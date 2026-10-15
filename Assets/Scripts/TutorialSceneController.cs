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
            showingTutorial = false;

            // Allow the player to click the campaign button.
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
