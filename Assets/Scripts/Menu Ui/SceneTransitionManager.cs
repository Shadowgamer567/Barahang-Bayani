using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    public static bool IsSceneTransitionPending { get; private set; }

    public static event Action SceneTransitionStarted;

    [Header("UI References")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private GameObject loadingAnimation;

    [Header("Settings")]
    [SerializeField, Min(0f)] private float fadeDuration = 0.5f;

    [Header("Disclaimer")]
    [SerializeField] private GameObject disclaimerText;
    [SerializeField, Min(0f)] private float disclaimerDisplayDuration = 6f;

    private bool isTransitioning;
    private bool destinationReady;
    private bool waitForDestinationReady;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (fadeCanvasGroup == null)
        {
            Debug.LogError("[Scene Transition] BlackScreen CanvasGroup is missing.");

            enabled = false;
            return;
        }

        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.interactable = false;
        fadeCanvasGroup.blocksRaycasts = false;

        if (loadingAnimation != null)
        {
            loadingAnimation.SetActive(false);
        }
    }

    private IEnumerator Start()
    {
        // Keep the screen black while the disclaimer is displayed.
        fadeCanvasGroup.alpha = 1f;
        fadeCanvasGroup.blocksRaycasts = true;
        fadeCanvasGroup.interactable = false;

        if (disclaimerText != null)
        {
            disclaimerText.SetActive(true);
        }

        else
        {
            Debug.LogError("[Scene Transition] DisclaimerText is missing.");
        }

        // Display the disclaimer.
        yield return new WaitForSecondsRealtime(disclaimerDisplayDuration);

        // Hide the disclaimer before revealing the Main Menu.
        if (disclaimerText != null)
        {
            disclaimerText.SetActive(false);
        }

        // Reveal the Main Menu.
        yield return FadeTo(0f);

        fadeCanvasGroup.blocksRaycasts = false;
        fadeCanvasGroup.interactable = false;
    }

    public void SwitchScene(string sceneName)
    {
        SwitchScene(sceneName, false);
    }

    public void SwitchScene(string sceneName, bool requireReadySignal)
    {
        if (!enabled || isTransitioning)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(sceneName) || !Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError("[Scene Transition] Invalid scene: " + sceneName);
            return;
        }

        isTransitioning = true;
        IsSceneTransitionPending = true;

        destinationReady = false;
        waitForDestinationReady = requireReadySignal;

        SceneTransitionStarted?.Invoke();

        StartCoroutine(TransitionRoutine(sceneName));
    }

    public void NotifySceneReady(string sceneName)
    {
        if (!isTransitioning)
        {
            return;
        }

        if (SceneManager.GetActiveScene().name != sceneName)
        {
            return;
        }

        destinationReady = true;

        Debug.Log("[Scene Transition] Scene ready: " + sceneName);
    }

    private IEnumerator TransitionRoutine(string sceneName)
    {
        // Cover the current scene with black.
        fadeCanvasGroup.blocksRaycasts = true;

        yield return FadeTo(1f);

        // Show the loading animation after the screen is black.
        if (loadingAnimation != null)
        {
            loadingAnimation.SetActive(true);
        }

        // Load the destination behind the black overlay.
        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName);
        
        if (loadOperation == null)
        {
            Debug.LogError("[Scene Transition] Failed to load: " + sceneName);

            yield return FinishTransition();
            yield break;
        }

        while (!loadOperation.isDone)
        {
            yield return null;
        }

        // Give the newly loaded scene a frame to begin.
        yield return null;

        // For scenes with custom initialization, wait for
        // their explicit readiness notification.
        if (waitForDestinationReady)
        {
            while (!destinationReady)
            {
                yield return null;
            }
        }

        yield return FinishTransition();
    }


    private IEnumerator FinishTransition()
    {
        Debug.Log("[Scene Transition] Starting fade-out. Alpha: " + fadeCanvasGroup.alpha);

        if (loadingAnimation != null)
            loadingAnimation.SetActive(false);

        yield return FadeTo(0f);

        Debug.Log("[Scene Transition] Fade-out finished. Alpha: " + fadeCanvasGroup.alpha);

        fadeCanvasGroup.blocksRaycasts = false;
        fadeCanvasGroup.interactable = false;

        isTransitioning = false;
        IsSceneTransitionPending = false;

        Debug.Log("[Scene Transition] Transition completed.");
    }


    private IEnumerator FadeTo(float targetAlpha)
    {
        float startAlpha = fadeCanvasGroup.alpha;

        if (fadeDuration <= 0f)
        {
            fadeCanvasGroup.alpha = targetAlpha;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Mathf.Min(Time.unscaledDeltaTime, 0.05f);

            float progress =
                Mathf.Clamp01(elapsed / fadeDuration);

            // Smooth easing.
            progress = Mathf.SmoothStep(0f, 1f, progress);

            fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, progress);

            yield return null;
        }

        fadeCanvasGroup.alpha = targetAlpha;
    }

    private void OnDestroy()
    {
        if (Instance != this)
            return;

        Instance = null;
        IsSceneTransitionPending = false;
    }
}
