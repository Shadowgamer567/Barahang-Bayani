using UnityEngine;
using UnityEngine.UI;

public class TutorialVictoryController : MonoBehaviour
{
    [Header("References")]
    public LevelManager levelManager;

    [Header("Tutorial UI")]
    public GameObject tutorialOverlay;
    public Button tutorialClickZoneButton;
    public GameObject[] tutorialPanels;
    public GameObject[] panelSpotlights;

    private int currentPanelIndex = 0;
    private bool showingTutorial = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (levelManager == null)
        {
            levelManager = FindFirstObjectByType<LevelManager>();
        }

        if (levelManager != null)
        {
            levelManager.OnVictoryScreenOpened += OnVictoryScreenOpened;
        }
        else
        {
            Debug.LogError("Victory Tutorial: LevelManager not found!");
        }

        tutorialClickZoneButton.onClick.AddListener(NextPanel);

        // Keep the tutorial hidden until the Victory Screen opens.
        tutorialOverlay.SetActive(false);
        tutorialClickZoneButton.gameObject.SetActive(false);

        foreach (GameObject panel in tutorialPanels)
        {
            if (panel != null)
                panel.SetActive(false);
        }

        foreach (GameObject spotlight in panelSpotlights)
        {
            if (spotlight != null)
                spotlight.SetActive(false);
        }
    }

    private void OnVictoryScreenOpened()
    {
        Debug.Log("Victory Tutorial: Victory Screen detected.");

        showingTutorial = true;
        currentPanelIndex = 0;

        tutorialClickZoneButton.gameObject.SetActive(true);

        ShowCurrentPanel();

        Debug.Log("Victory Tutorial: Started.");
    }

    private void ShowCurrentPanel()
    {
        // The full-screen overlay is only used for Panel 0.
        tutorialOverlay.SetActive(currentPanelIndex == 0);

        if (currentPanelIndex >= tutorialPanels.Length)
            return;

        tutorialPanels[currentPanelIndex].SetActive(true);

        // Disable all spotlights first.
        foreach (GameObject spotlight in panelSpotlights)
        {
            if (spotlight != null)
                spotlight.SetActive(false);
        }

        // Enable the spotlight assigned to the current panel.
        if (currentPanelIndex < panelSpotlights.Length &&
            panelSpotlights[currentPanelIndex] != null)
        {
            panelSpotlights[currentPanelIndex].SetActive(true);
        }

        if (currentPanelIndex == 5 && panelSpotlights.Length > 5 && panelSpotlights[5] != null)
        {
            GameObject spotlight = panelSpotlights[5];

            Debug.Log("Panel 5 Spotlight activeSelf = " + spotlight.activeSelf);
            Debug.Log("Panel 5 Spotlight activeInHierarchy = " + spotlight.activeInHierarchy);
            Debug.Log("Panel 5 Spotlight parent = " +
                (spotlight.transform.parent != null
                    ? spotlight.transform.parent.name
                    : "None"));
        }

        if (currentPanelIndex == 8)
        {
            showingTutorial = false;
            tutorialClickZoneButton.gameObject.SetActive(false);

            Debug.Log("Victory Tutorial: Panel 8 displayed. Waiting for Return.");
        }
    }

    private void NextPanel()
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
        else
        {
            FinishTutorial();
        }
    }

    private void FinishTutorial()
    {
        showingTutorial = false;

        tutorialOverlay.SetActive(false);
        tutorialClickZoneButton.gameObject.SetActive(false);

        foreach (GameObject spotlight in panelSpotlights)
        {
            if (spotlight != null)
                spotlight.SetActive(false);
        }

        Debug.Log("Victory Tutorial: Finished.");
    }

    void OnDestroy()
    {
        if (levelManager != null)
        {
            levelManager.OnVictoryScreenOpened -= OnVictoryScreenOpened;
        }

        if (tutorialClickZoneButton != null)
        {
            tutorialClickZoneButton.onClick.RemoveListener(NextPanel);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
