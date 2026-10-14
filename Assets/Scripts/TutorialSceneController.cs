using UnityEngine;
using UnityEngine.InputSystem;

public class TutorialSceneController : MonoBehaviour
{
    public GameObject tutorialPanel;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (TutorialManager.Instance == null)
        {
            return;
        }

        if (TutorialManager.Instance.tutorialActive &&
            TutorialManager.Instance.currentStage == TutorialStage.MapIntroduction)
        {
            tutorialPanel.SetActive(true);
        }
        else
        {
            tutorialPanel.SetActive(false);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!tutorialPanel.activeSelf)
        {
            return;
        }

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            tutorialPanel.SetActive(false);

            TutorialManager.Instance.AdvanceStage(TutorialStage.SelectTutorialCampaign);
        }
    }
}
