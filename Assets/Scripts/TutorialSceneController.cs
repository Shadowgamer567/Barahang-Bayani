using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class TutorialSceneController : MonoBehaviour
{
    public GameObject[] tutorialPanels;

    public GameObject tutorialOverlay;
    public GameObject tutorialSpotlight;
    public Button tutorialClickZoneButton;


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
        tutorialSpotlight.SetActive(false);
        tutorialClickZoneButton.gameObject.SetActive(false);

        if (TutorialManager.Instance == null)
        {
            return;
        }

        if (TutorialManager.Instance.tutorialActive && TutorialManager.Instance.currentStage == TutorialStage.MapIntroduction)
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

        if (currentPanelIndex < 2)
        {
            tutorialOverlay.SetActive(true);
            tutorialSpotlight.SetActive(false);
        }

        else
        {
            tutorialOverlay.SetActive(false);
            tutorialSpotlight.SetActive(true);
        }

        if (currentPanelIndex == tutorialPanels.Length - 1)
        {
            showingTutorial = false;

            // Allow the player to click the campaign button.
            tutorialClickZoneButton.gameObject.SetActive(false);

            TutorialManager.Instance.AdvanceStage(TutorialStage.SelectTutorialCampaign);

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
}
