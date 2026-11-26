using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelManager : MonoBehaviour
{
    public LevelData levelData;
    public BattleControl battleControl;
    public LevelData debugLevelData;

    public GameObject mainUI;
    public GameObject victoryUI;
    public GameObject defeatUI;
    private CutsceneManager cutsceneManager;

    [Header("Level Location Title")]
    [SerializeField] private TMP_FontAsset locationTitleFont;
    [SerializeField, Min(0.01f)] private float locationFadeDuration = 0.7f;
    [SerializeField, Min(0f)] private float locationHoldDuration = 1.8f;
    [SerializeField, Min(1f)] private float locationTitleFontSize = 108f;

    private int battlesCompleted = 0;

    public event Action OnVictoryScreenOpened;

    void Start()
    {
        AchievementManager.BeginLevel();

        if (GameManager.Instance != null && GameManager.Instance.selectedLevel != null)
        {
            levelData = GameManager.Instance.selectedLevel;
        }

        else
        {
            Debug.LogWarning("Using DEBUG LevelData");
            levelData = debugLevelData;

            GameManager.Instance.selectedLevel = levelData;
        }

        Debug.Log("LevelManager ID: " + GetInstanceID() + " | Loaded LevelData: " + (levelData != null ? levelData.name : "NULL"));

        if (levelData == null)
        {
            Debug.LogError("No LevelData assigned!");
            return;
        }

        QuizManager quizManager = FindFirstObjectByType<QuizManager>();

        if (quizManager != null)
        {
            quizManager.LoadQuestions(levelData.campaign);
        }
        else
        {
            Debug.LogError("QuizManager not found in the Level scene!");
        }

        BackgroundLoop bg = FindFirstObjectByType<BackgroundLoop>();

        if (bg != null)
        {
            bg.currentLevel = levelData.levelIndex;
            bg.currentCampaign = levelData.campaign;

            Debug.Log("Background set to level: " + bg.currentLevel + " Campaign set to: " + bg.currentCampaign.name);

            bg.InitializeTiles();
        }

        else
        {
            Debug.LogWarning("BackgroundLoop not found");
        }

        GroundLoop gl = FindFirstObjectByType<GroundLoop>();

        if (gl != null)
        {
            Material groundMat = levelData.campaign.GetGroundMaterial(levelData.levelIndex);

            if (groundMat != null)
            {
                gl.ApplyGroundMaterial(groundMat);
            }
        }


        battleControl = FindFirstObjectByType<BattleControl>();

        if (battleControl == null)
        {
            Debug.LogError("BattleControl not assigned!");
            return;
        }

        battleControl.Initialize(levelData);

        battleControl.OnBattleWon += HandleBattleWon;
        battleControl.OnBattleLost += HandleBattleLost;

        Debug.Log("Loaded Level: " + levelData.levelName);
        Debug.Log("Battles Required: " + levelData.battlesRequired);

        //Reset Victory UI Quiz Stats
        QuizStats.Instance?.ResetStats();

        cutsceneManager = FindFirstObjectByType<CutsceneManager>();

        // Initial level setup is complete.
        // Notify the transition manager that Level can be revealed.
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.NotifySceneReady("Level");
        }

        //Wait for Transition to finish before starting opening cutscene
        StartCoroutine(StartLevelAfterTransition());
    }

    void HandleBattleWon()
    {
        Debug.Log("HandleBattleWon Called");

        if (levelData == null)
        {
            Debug.LogError("levelData is Null");
            return;
        }

        battlesCompleted++;

        Debug.Log($"Progress: {battlesCompleted}/{levelData.battlesRequired}");

        if (battlesCompleted >= levelData.battlesRequired)
        {
            LevelComplete();
        }

        else
        {
            TriggerCutscene(CutsceneTriggerType.AfterBattle, battlesCompleted, () =>
            {
                battleControl.StartBattleSequence();
            });
        }
    }

    void HandleBattleLost()
    {
        Debug.Log("LevelManager: HandleBattleLost Called");

        ShowDefeatScreen();
    }

    void OnDestroy()
    {
        if (battleControl != null)
        {
            battleControl.OnBattleWon -= HandleBattleWon;
            battleControl.OnBattleLost -= HandleBattleLost;
        }
    }

    void LevelComplete()
    {
        Debug.Log("LEVEL COMPLETE");

        if (ProgressManager.Instance == null)
        {
            Debug.LogError("ProgressManager is Null");
            return;
        }

        if (levelData == null)
        {
            Debug.LogError("levelData is Null in LevelComplete");
            return;
        }

        ProgressManager.Instance.CompleteLevel(levelData.levelName);

        TriggerCutscene(CutsceneTriggerType.End, 0, ShowVictoryScreen);
    }

    public void ShowVictoryScreen()
    {
        Time.timeScale = 0f;

        battleControl = FindFirstObjectByType<BattleControl>();

        if (battleControl != null)
        {
            battleControl.isLevelFinished = true;
            battleControl.StopAllCoroutines();
        }

        if (mainUI != null)
        {
            mainUI.SetActive(false);
        }

        if (victoryUI != null)
        {
            victoryUI.SetActive(true);
        }

        OnVictoryScreenOpened?.Invoke();

        Debug.Log("LevelManager: Victory Screen opened.");
    }

    public void ShowDefeatScreen()
    {
        Debug.Log("LevelManager: Opening Defeat Screen.");

        Time.timeScale = 0f;

        if (battleControl != null)
        {
            battleControl.isLevelFinished = true;
            battleControl.StopAllCoroutines();
        }

        if (mainUI != null)
        {
            mainUI.SetActive(false);
        }

        if (victoryUI != null)
        {
            victoryUI.SetActive(false);
        }

        if (defeatUI != null)
        {
            defeatUI.SetActive(true);
        }
        else
        {
            Debug.LogError("LevelManager: DefeatUI is not assigned!");
        }

        Debug.Log("LevelManager: Defeat Screen opened.");
    }


    void TriggerCutscene(CutsceneTriggerType type, int battleIndex, System.Action onFinished = null)
    {
        if (levelData.Cutscenes == null || cutsceneManager == null)
        {
            onFinished?.Invoke();
            return;
        }

        foreach (var trigger in levelData.Cutscenes)
        {
            if (trigger.triggerType == type)
            {
                if (type == CutsceneTriggerType.AfterBattle && trigger.battleIndex != battleIndex)
                {
                    continue;
                }

                cutsceneManager.OnCutsceneFinished = () =>
                {
                    onFinished?.Invoke();
                };

                cutsceneManager.PlayCutscene(trigger.Cutscene);

                return;
            }
        }

        onFinished?.Invoke();
    }

    void StartFirstBattle()
    {
        Debug.Log("Starting First Battle");

        if (battleControl != null)
        {
            battleControl.StartBattleSequence();
        }
    }
    void ResumeNextBattle()
    {
        Debug.Log("Cutscene finished => starting next battle");

        if (battleControl != null)
        {
            battleControl.StartNextBattleManually();
        }
    }

    private IEnumerator StartLevelAfterTransition()
    {
        // Wait while the transition manager is covering the screen.
        while (SceneTransitionManager.IsSceneTransitionPending)
        {
            yield return null;
        }

        // Begin the opening cutscene or first battle
        // only after the Level is fully visible.
        TriggerCutscene(CutsceneTriggerType.Start, 0, ShowLocationThenStartBattle);
    }

    private void ShowLocationThenStartBattle()
    {
        string location = GetLevelLocation();
        if (string.IsNullOrEmpty(location))
        {
            battleControl.StartBattleSequence();
            return;
        }

        StartCoroutine(ShowLocationTitleCard(location, "LEVEL " + levelData.levelIndex));
    }

    private string GetLevelLocation()
    {
        if (levelData == null) return "";

        int index = levelData.levelIndex;
        if (index >= 1 && index <= 5)
        {
            return "LAGUNA";
        }
        if (index >= 6 && index <= 9)
        {
            return "MANILA";
        }
        if (index >= 10 && index <= 15)
        {
            return "BAGUMBAYAN";
        }
        return "";
    }

    private IEnumerator ShowLocationTitleCard(string location, string levelLabel)
    {
        GameObject canvasObject = new GameObject("Level Location Title Canvas",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 500;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        GameObject panel = new GameObject("Location Title", typeof(RectTransform), typeof(CanvasGroup));
        panel.transform.SetParent(canvasObject.transform, false);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(1100f, 300f);

        TMP_FontAsset font = locationTitleFont != null
            ? locationTitleFont
            : TMP_Settings.defaultFontAsset;
        TextMeshProUGUI title = CreateLocationText("Location", panel.transform,
            location, locationTitleFontSize, font, new Vector2(0f, 32f), new Vector2(1080f, 170f));
        title.fontStyle = FontStyles.Bold;
        title.color = new Color(1f, 0.91f, 0.69f, 1f);
        title.outlineWidth = 0.25f;
        title.outlineColor = Color.black;

        TextMeshProUGUI range = CreateLocationText("Current Level", panel.transform,
            levelLabel, 40f, font, new Vector2(0f, -95f), new Vector2(1000f, 80f));
        range.color = new Color(1f, 0.97f, 0.89f, 1f);
        range.characterSpacing = 5f;
        range.outlineWidth = 0.2f;
        range.outlineColor = Color.black;

        CanvasGroup group = panel.GetComponent<CanvasGroup>();
        group.alpha = 0f;
        yield return FadeLocation(group, 0f, 1f, locationFadeDuration);
        yield return new WaitForSecondsRealtime(locationHoldDuration);
        yield return FadeLocation(group, 1f, 0f, locationFadeDuration);
        Destroy(canvasObject);
        battleControl.StartBattleSequence();
    }

    private TextMeshProUGUI CreateLocationText(string objectName, Transform parent,
        string text, float fontSize, TMP_FontAsset font, Vector2 position, Vector2 size)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        if (font != null) label.font = font;
        return label;
    }

    private static IEnumerator FadeLocation(CanvasGroup group, float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            group.alpha = Mathf.Lerp(from, to, t * t * (3f - 2f * t));
            yield return null;
        }
        group.alpha = to;
    }


    // Update is called once per frame
    void Update()
    {
        
    }
}
