using UnityEngine;

public enum TutorialStage
{
    NotStarted,
    MapIntroduction,
    SelectTutorialCampaign,
    LevelSelectIntroduction,
    ExploreScrollableContent,
    SelectTutorialLevel,
    BattleTutorial,
    VictoryTutorial,
    ReturnToLevelSelect,
    FinalExplanation,
    Completed
}

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;

    public TutorialStage currentStage = TutorialStage.NotStarted;

    public bool tutorialActive = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void StartTutorial()
    {
        tutorialActive = true;
        currentStage = TutorialStage.MapIntroduction;

        Debug.Log("Tutorial started: " + currentStage);
    }

    public void AdvanceStage(TutorialStage nextStage)
    {
        if (!tutorialActive)
        {
            return;
        }

        Debug.Log(
            $"Tutorial advancing from {currentStage} to {nextStage}\n" +
            System.Environment.StackTrace
        );

        currentStage = nextStage;

        Debug.Log("Tutorial advanced to: " + currentStage);
    }

    public void CompleteTutorial()
    {
        currentStage = TutorialStage.Completed;
        tutorialActive = false;

        Debug.Log("Tutorial Completed"); 
    }
    void Start()
    {
        
    }

    void Update()
    {
        
    }
}