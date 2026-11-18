
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WebsiteStudentCardUI : MonoBehaviour
{
    [Header("Card UI")]
    public TextMeshProUGUI usernameText;
    public Image borderImage;

    [Header("Selection Colors")]
    public Color normalColor = Color.white;
    public Color selectedColor = Color.yellow;

    private Button button;

    private string studentAccountId;
    private Action<string> onStudentSelected;

    public void Setup(
        string accountId,
        string username,
        Action<string> selectionCallback)
    {
        studentAccountId = accountId;
        onStudentSelected = selectionCallback;

        if (usernameText != null)
            usernameText.text = username;

        button = GetComponent<Button>();

        if (button == null)
        {
            Debug.LogError(
                "[WebsiteStudentCard] Button component is missing.",
                this
            );
            return;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnClick);

        SetSelected(false);
    }

    private void OnClick()
    {
        if (string.IsNullOrWhiteSpace(studentAccountId))
        {
            Debug.LogError(
                "[WebsiteStudentCard] Student account ID is missing."
            );
            return;
        }

        onStudentSelected?.Invoke(studentAccountId);
    }

    public void SetSelected(bool selected)
    {
        if (borderImage != null)
        {
            borderImage.color =
                selected ? selectedColor : normalColor;
        }
    }

    public string GetStudentAccountId()
    {
        return studentAccountId;
    }
}
