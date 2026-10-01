using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : MonoBehaviour
{
    private enum TransitionMode
    {
        FadeOutBeforeLoad,
        FadeInAfterLoad,
        FadeOutAndIn
    }

    public static SceneTransitionManager Instance;
    private static bool sceneTransitionPending;
    private static TransitionMode activeTransitionMode;
    public static bool IsSceneTransitionPending => sceneTransitionPending;
    public static event System.Action SceneTransitionStarted;

    [Header("UI References")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;

    [Header("Settings")]
    [SerializeField] private TransitionMode transitionMode = TransitionMode.FadeOutAndIn;
    [SerializeField] private float fadeDuration = 0.5f;

    private bool isTransitioning;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        if (fadeCanvasGroup == null)
        {
            fadeCanvasGroup = GetComponent<CanvasGroup>();
            if (fadeCanvasGroup == null)
            {
                fadeCanvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        if (sceneTransitionPending)
        {
            bool fadeInAfterLoad = activeTransitionMode != TransitionMode.FadeOutBeforeLoad;
            fadeCanvasGroup.alpha = fadeInAfterLoad ? 0f : 1f;
            fadeCanvasGroup.interactable = !fadeInAfterLoad;
            fadeCanvasGroup.blocksRaycasts = !fadeInAfterLoad;
        }
    }

    private void Start()
    {
        if (!sceneTransitionPending)
        {
            return;
        }

        if (activeTransitionMode == TransitionMode.FadeOutBeforeLoad)
        {
            CompleteTransition();
        }
        else if (fadeCanvasGroup != null)
        {
            StartCoroutine(FadeIn());
        }
        else
        {
            CompleteTransition();
        }
    }

    public void SwitchScene(string sceneName)
    {
        if (isTransitioning)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(sceneName) || !Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"Cannot load scene '{sceneName}'. Check that it is included in Build Settings.");
            return;
        }

        isTransitioning = true;
        activeTransitionMode = transitionMode;
        sceneTransitionPending = true;
        SceneTransitionStarted?.Invoke();
        StartCoroutine(TransitionRoutine(sceneName));
    }

    private IEnumerator TransitionRoutine(string sceneName)
    {
        bool shouldFadeOut = activeTransitionMode != TransitionMode.FadeInAfterLoad;
        if (shouldFadeOut && fadeCanvasGroup != null)
        {
            yield return StartCoroutine(FadeOut());
        }

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        if (asyncLoad == null)
        {
            Debug.LogError($"Failed to start loading scene '{sceneName}'.");
            if (fadeCanvasGroup != null)
            {
                fadeCanvasGroup.alpha = 1f;
                fadeCanvasGroup.interactable = true;
                fadeCanvasGroup.blocksRaycasts = true;
            }
            CompleteTransition();
            yield break;
        }

        while (!asyncLoad.isDone)
        {
            yield return null;
        }
    }

    private IEnumerator FadeOut()
    {
        float timer = 0f;
        float startAlpha = fadeCanvasGroup.alpha;
        fadeCanvasGroup.interactable = false;
        fadeCanvasGroup.blocksRaycasts = true;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, Mathf.Clamp01(timer / fadeDuration));
            yield return null;
        }
        fadeCanvasGroup.alpha = 0f;
    }

    private IEnumerator FadeIn()
    {
        float timer = 0f;
        float startAlpha = fadeCanvasGroup.alpha;
        fadeCanvasGroup.interactable = false;
        fadeCanvasGroup.blocksRaycasts = true;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, Mathf.Clamp01(timer / fadeDuration));
            yield return null;
        }
        fadeCanvasGroup.alpha = 1f;
        fadeCanvasGroup.interactable = true;
        fadeCanvasGroup.blocksRaycasts = true;
        CompleteTransition();
    }

    private void CompleteTransition()
    {
        sceneTransitionPending = false;
        isTransitioning = false;
    }
}