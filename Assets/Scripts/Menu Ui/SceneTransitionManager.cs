using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance;
    private static bool sceneTransitionPending;

    [Header("UI References")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;

    [Header("Settings")]
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
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.interactable = false;
            fadeCanvasGroup.blocksRaycasts = false;
        }
    }

    private void Start()
    {
        if (sceneTransitionPending)
        {
            StartCoroutine(FadeIn());
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
    sceneTransitionPending = true;
        StartCoroutine(TransitionRoutine(sceneName));
    }

    private IEnumerator TransitionRoutine(string sceneName)
    {
        // 1. Fade to Black
        if (fadeCanvasGroup != null)
        {
            yield return StartCoroutine(FadeOut());
        }

        // 2. Load the scene asynchronously
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        if (asyncLoad == null)
        {
            Debug.LogError($"Failed to start loading scene '{sceneName}'.");
            if (fadeCanvasGroup != null)
            {
                yield return StartCoroutine(FadeIn());
            }
            isTransitioning = false;
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
        fadeCanvasGroup.blocksRaycasts = false;
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
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Clamp01(timer / fadeDuration);
            yield return null;
        }
        fadeCanvasGroup.alpha = 1f;
        fadeCanvasGroup.interactable = true;
        fadeCanvasGroup.blocksRaycasts = true;
        sceneTransitionPending = false;
        isTransitioning = false;
    }
}