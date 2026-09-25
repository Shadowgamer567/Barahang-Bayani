using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardShufflerUI : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private float shuffleDuration = 1.2f;
    [SerializeField] private float splitDistance = 180f;
    [SerializeField] private float arcHeight = 40f;
    [SerializeField] private int shufflePasses = 3;

    private List<RectTransform> cardTransforms = new List<RectTransform>();
    private List<Vector2> originalPositions = new List<Vector2>();

    private int topCardIndex = 0;
    private bool isShuffling = false;

    private void Awake()
    {
        InitializeDeck();
    }

    private void OnEnable()
    {
        StartShuffle();
    }

    private void InitializeDeck()
    {
        cardTransforms.Clear();
        originalPositions.Clear();

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child.TryGetComponent(out RectTransform cardRect))
            {
                cardTransforms.Add(cardRect);
                originalPositions.Add(cardRect.anchoredPosition);
                child.gameObject.SetActive(true);
            }
        }
    }

    public void StartShuffle()
    {
        if (isShuffling || cardTransforms.Count == 0) return;

        InitializeDeck();
        StartCoroutine(ShuffleRoutine());
    }

    public void ShowNextCard()
    {
        if (isShuffling || cardTransforms.Count == 0) return;

        topCardIndex = (topCardIndex + 1) % cardTransforms.Count;

        if (cardTransforms[topCardIndex] != null)
        {
            cardTransforms[topCardIndex].anchoredPosition = originalPositions[topCardIndex];
            cardTransforms[topCardIndex].SetAsLastSibling();
        }
    }

    private void RotateDeckOrder()
    {
        if (cardTransforms.Count <= 1)
            return;

        RectTransform lastCard = cardTransforms[cardTransforms.Count - 1];

        cardTransforms.RemoveAt(cardTransforms.Count - 1);
        cardTransforms.Insert(0, lastCard);

        lastCard.SetAsFirstSibling();
    }

    private IEnumerator ShuffleRoutine()
    {
        isShuffling = true;

        for (int pass = 0; pass < shufflePasses; pass++)
        {
            float splitTimer = 0f;
            float splitTime = shuffleDuration * 0.25f;

            while (splitTimer < splitTime)
            {
                splitTimer += Time.deltaTime;
                float progress = Mathf.SmoothStep(0f, 1f, splitTimer / splitTime);

                for (int i = 0; i < cardTransforms.Count; i++)
                {
                    float dir = (i % 2 == 0) ? -1f : 1f;
                    Vector2 targetPos = originalPositions[i] + new Vector2(dir * splitDistance, 0f);
                    cardTransforms[i].anchoredPosition = Vector2.Lerp(originalPositions[i], targetPos, progress);
                }

                yield return null;
            }

            float gatherTimer = 0f;
            float gatherTime = shuffleDuration * 0.5f;

            while (gatherTimer < gatherTime)
            {
                gatherTimer += Time.deltaTime;
                float progress = gatherTimer / gatherTime;

                for (int i = 0; i < cardTransforms.Count; i++)
                {
                    float staggerDelay = (float)i / cardTransforms.Count * 0.3f;
                    float cardProgress = Mathf.Clamp01((progress - staggerDelay) / (1f - staggerDelay));
                    cardProgress = Mathf.SmoothStep(0f, 1f, cardProgress);

                    float dir = (i % 2 == 0) ? -1f : 1f;
                    Vector2 splitPos = originalPositions[i] + new Vector2(dir * splitDistance, 0f);

                    float heightOffset = Mathf.Sin(cardProgress * Mathf.PI) * arcHeight;
                    Vector2 currentPos = Vector2.Lerp(splitPos, originalPositions[i], cardProgress);
                    currentPos.y += heightOffset;

                    cardTransforms[i].anchoredPosition = currentPos;
                }

                yield return null;
            }

            RotateDeckOrder();
            yield return new WaitForSeconds(0.1f);
        }

        for (int i = 0; i < cardTransforms.Count; i++)
        {
            cardTransforms[i].anchoredPosition = originalPositions[i];
        }

        RotateDeckOrder();

        isShuffling = false;
    }
}