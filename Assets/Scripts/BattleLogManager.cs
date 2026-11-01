
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Text.RegularExpressions;

public class BattleLogManager : MonoBehaviour
{
    [Header("UI References")]
    public ScrollRect battleLogScrollRect;
    public Transform battleLogContent;
    public TextMeshProUGUI battleLogTemplate;

    [Header("Log Settings")]
    public int maxEntries = 50;

    [Header("Entry Animation")]
    public float slideDuration = 0.3f;
    public float slideDistance = 80f;

    private readonly Queue<GameObject> entries = new Queue<GameObject>();

    private readonly List<Coroutine> entryAnimations = new List<Coroutine>();

    private Coroutine scrollCoroutine;

    public void AddLog(string message)
    {
        if (battleLogContent == null || battleLogTemplate == null)
        {
            Debug.LogWarning("Battle Log references are missing.");
            return;
        }

        // Create a layout-controlled container.
        GameObject container = new GameObject("BattleLogEntry", typeof(RectTransform), typeof(LayoutElement));

        container.transform.SetParent(battleLogContent, false);

        RectTransform containerRect = container.GetComponent<RectTransform>();

        LayoutElement layoutElement = container.GetComponent<LayoutElement>();

        // Create the text inside the container.
        TextMeshProUGUI newEntry = Instantiate(battleLogTemplate, container.transform);

        newEntry.gameObject.SetActive(true);
        newEntry.richText = true;
        newEntry.text = ColorizeLog(message);

        RectTransform textRect = newEntry.rectTransform;

        // Stretch text horizontally inside the container.
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        // Determine the text height based on the available width.
        Canvas.ForceUpdateCanvases();

        float width = containerRect.rect.width;

        if (width <= 0f && battleLogContent is RectTransform contentRect)
        {
            width = contentRect.rect.width;
        }

        width = Mathf.Max(1f, width);

        float height = newEntry.GetPreferredValues(newEntry.text, width, 0f).y;

        layoutElement.preferredHeight = height;
        layoutElement.flexibleHeight = 0f;

        entries.Enqueue(container);

        // Remove the oldest entries.
        while (entries.Count > Mathf.Max(1, maxEntries))
        {
            GameObject oldest = entries.Dequeue();

            if (oldest != null)
            {
                oldest.SetActive(false);
                Destroy(oldest);
            }
        }

        // Animate the text, not the layout container.
        Coroutine animation = StartCoroutine(AnimateEntry(textRect));

        entryAnimations.Add(animation);

        // Scroll to the newest entry.
        if (scrollCoroutine != null)
        {
            StopCoroutine(scrollCoroutine);
        }

        scrollCoroutine = StartCoroutine(ScrollToBottom());
    }

    private IEnumerator AnimateEntry(RectTransform textRect)
    {
        float elapsed = 0f;

        Vector2 startPosition = new Vector2(slideDistance, 0f);

        textRect.anchoredPosition = startPosition;

        CanvasGroup canvasGroup = textRect.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup = textRect.gameObject.AddComponent<CanvasGroup>();
        }

        canvasGroup.alpha = 0f;

        while (elapsed < slideDuration)
        {
            if (textRect == null)
            {
                yield break;
            }

            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.001f, slideDuration)
            );

            // Smoothstep easing.
            t = t * t * (3f - 2f * t);

            textRect.anchoredPosition = Vector2.Lerp(startPosition, Vector2.zero, t);

            canvasGroup.alpha = t;

            yield return null;
        }

        if (textRect != null)
        {
            textRect.anchoredPosition = Vector2.zero;
            canvasGroup.alpha = 1f;
        }
    }

    private IEnumerator ScrollToBottom()
    {
        yield return null;

        Canvas.ForceUpdateCanvases();

        if (battleLogScrollRect != null)
        {
            battleLogScrollRect.verticalNormalizedPosition = 0f;
        }

        scrollCoroutine = null;
    }

    public void ClearLog()
    {
        if (scrollCoroutine != null)
        {
            StopCoroutine(scrollCoroutine);
            scrollCoroutine = null;
        }

        StopAllCoroutines();
        entryAnimations.Clear();

        while (entries.Count > 0)
        {
            GameObject entry = entries.Dequeue();

            if (entry != null)
            {
                entry.SetActive(false);
                Destroy(entry);
            }
        }
    }


    private string ColorizeLog(string message)
    {
        string pattern =
            @"\b(?:removing \d+ Shield|" +
            @"dealing (?:up to )?\d+ (?:HP )?damage|" +
            @"restoring (?:up to )?\d+ HP|" +
            @"granting \d+ Shield|" +
            @"Correct answer|" +
            @"Incorrect answer|" +
            @"failed to activate|" +
            @"attacked|healed|healing|heal|" +
            @"shield|buff|debuff|increasing|reducing)\b";

        return Regex.Replace(message, pattern, match =>
            {
                string word = match.Value;
                string lower = word.ToLowerInvariant();

                string color;

                if (lower.Contains("incorrect") || lower.Contains("failed") || lower.Contains("damage") || lower.Contains("removing") ||
                    lower == "attacked")
                {
                    color = "#C94B45";
                }

                else if (lower.Contains("correct") || lower.Contains("restoring") || lower.Contains("heal"))
                {
                    color = "#348A45";
                }

                else if (lower.Contains("shield") || lower.Contains("granting"))
                {
                    color = "#387FBC";
                }

                else if (lower.Contains("buff") && !lower.Contains("debuff") || lower.Contains("increasing"))
                {
                    color = "#A47A21";
                }

                else
                {
                    color = "#8653B2";
                }

                string safeWord = word.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
                return $"<color={color}>{safeWord}</color>";
            },

            RegexOptions.IgnoreCase
        );
    }



    [ContextMenu("Test Battle Log")]
    public void TestBattleLog()
    {
        AddLog("You played Slash, dealing 5 damage to Soldier #1.");
        AddLog("Soldier #2 used Grant Shield, adding 5 Shield to Soldier #1.");
        AddLog("You played Healing Prayer, restoring 4 HP to Jose Rizal.");
        AddLog("Soldier #1 attacked Jose Rizal for 3 damage.");
    }
}
