
using System.Collections;
using TMPro;
using UnityEngine;

public class QuizMakerFeedback : MonoBehaviour
{
    [Header("Feedback UI")]
    [SerializeField] private GameObject feedbackPanel;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text feedbackText;

    [Header("Display Settings")]
    [SerializeField] private float displayDuration = 3f;
    [SerializeField] private float fadeDuration = 0.5f;

    private Coroutine feedbackRoutine;

    private void Awake()
    {
        if (canvasGroup == null)
        {
            canvasGroup = feedbackPanel.GetComponent<CanvasGroup>();
        }

        canvasGroup.alpha = 0f;
        feedbackPanel.SetActive(false);
    }

    public void ShowFeedback(string message)
    {
        if (feedbackRoutine != null)
        {
            StopCoroutine(feedbackRoutine);
        }

        feedbackText.text = message;

        feedbackPanel.SetActive(true);
        canvasGroup.alpha = 1f;

        feedbackRoutine = StartCoroutine(FeedbackRoutine());
    }

    private IEnumerator FeedbackRoutine()
    {
        yield return new WaitForSecondsRealtime(displayDuration);

        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);

            yield return null;
        }

        canvasGroup.alpha = 0f;
        feedbackPanel.SetActive(false);

        feedbackRoutine = null;
    }
}
