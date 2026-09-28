using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ScreenFade : MonoBehaviour
{
    private enum FadeDirection
    {
        FadeIn,
        FadeOut
    }

    [SerializeField] private GameObject targetObject;
    [SerializeField] private FadeDirection fadeDirection = FadeDirection.FadeOut;
    [SerializeField, Min(0)] private int visibleFrames = 3;
    [SerializeField, Min(0f)] private float fadeDuration = 0.5f;

    private CanvasGroup targetCanvasGroup;
    private bool targetContainsListener;
    private Coroutine fadeCoroutine;

    private void Awake()
    {
        if (targetObject == null)
        {
            targetObject = gameObject;
        }

        targetContainsListener = targetObject == gameObject || transform.IsChildOf(targetObject.transform);
        targetCanvasGroup = targetObject.GetComponent<CanvasGroup>();
        if (targetCanvasGroup == null)
        {
            targetCanvasGroup = targetObject.AddComponent<CanvasGroup>();
        }

        targetCanvasGroup.alpha = 0f;
        targetCanvasGroup.interactable = false;
        targetCanvasGroup.blocksRaycasts = false;

        if (!targetContainsListener)
        {
            targetObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneTransitionManager.SceneTransitionStarted += OnTransitionStarted;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneTransitionManager.SceneTransitionStarted -= OnTransitionStarted;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!SceneTransitionManager.IsSceneTransitionPending)
        {
            return;
        }

        SetTargetAlpha(GetStartAlpha());
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        fadeCoroutine = StartCoroutine(FadeTarget());
    }

    private void OnTransitionStarted()
    {
        SetTargetAlpha(GetStartAlpha());
    }

    private float GetStartAlpha()
    {
        return fadeDirection == FadeDirection.FadeIn ? 0f : 1f;
    }

    private float GetEndAlpha()
    {
        return fadeDirection == FadeDirection.FadeIn ? 1f : 0f;
    }

    private void SetTargetAlpha(float alpha)
    {
        if (!targetContainsListener)
        {
            targetObject.SetActive(true);
        }

        targetCanvasGroup.alpha = alpha;
        targetCanvasGroup.interactable = false;
        targetCanvasGroup.blocksRaycasts = true;
    }

    private IEnumerator FadeTarget()
    {
        for (int frame = 0; frame < visibleFrames; frame++)
        {
            yield return null;
        }

        while (SceneTransitionManager.IsSceneTransitionPending)
        {
            yield return null;
        }

        float timer = 0f;
        float startAlpha = GetStartAlpha();
        float endAlpha = GetEndAlpha();
        targetCanvasGroup.interactable = false;
        targetCanvasGroup.blocksRaycasts = true;

        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            targetCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, Mathf.Clamp01(timer / fadeDuration));
            yield return null;
        }

        targetCanvasGroup.alpha = 0f;
        targetCanvasGroup.interactable = false;
        targetCanvasGroup.blocksRaycasts = false;
        if (!targetContainsListener)
        {
            targetObject.SetActive(false);
        }

        fadeCoroutine = null;
    }
}
