
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PasswordVisibility : MonoBehaviour
{
    [Header("Password")]
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private TMP_Text passwordDisplay;
    [SerializeField] private float revealDuration = 1f;

    [Header("Custom Caret")]
    [SerializeField] private Image passwordCaret;
    [SerializeField] private float blinkInterval = 0.5f;

    private Coroutine hideCoroutine;

    private bool isPasswordVisible = false;
    private bool isSelected = false;

    private void OnEnable()
    {
        passwordInput.onValueChanged.AddListener(UpdatePassword);
        passwordInput.onSelect.AddListener(OnInputSelected);
        passwordInput.onDeselect.AddListener(OnInputDeselected);

        UpdatePassword(passwordInput.text);

        if (passwordCaret != null)
        {
            passwordCaret.gameObject.SetActive(false);
        }
    }

    private void OnDisable()
    {
        passwordInput.onValueChanged.RemoveListener(UpdatePassword);
        passwordInput.onSelect.RemoveListener(OnInputSelected);
        passwordInput.onDeselect.RemoveListener(OnInputDeselected);

        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
        }
    }

    private void UpdatePassword(string password)
    {
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }

        if (isPasswordVisible)
        {
            passwordDisplay.text = password;
        }

        else if (string.IsNullOrEmpty(password))
        {
            passwordDisplay.text = "";
        }

        else
        {
            passwordDisplay.text =
                new string('•', password.Length - 1) +
                password[password.Length - 1];

            hideCoroutine = StartCoroutine(HideLastCharacter(password));
        }

        UpdateCaretPosition();
    }

    private IEnumerator HideLastCharacter(string password)
    {
        yield return new WaitForSecondsRealtime(revealDuration);

        if (!isPasswordVisible)
        {
            passwordDisplay.text = new string('•', password.Length);
            UpdateCaretPosition();
        }

        hideCoroutine = null;
    }

    public void SetPasswordVisible(bool visible)
    {
        isPasswordVisible = visible;

        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }

        if (isPasswordVisible)
        {
            passwordDisplay.text = passwordInput.text;
        }

        else
        {
            passwordDisplay.text = new string('•', passwordInput.text.Length);
        }

        UpdateCaretPosition();
    }

    private void OnInputSelected(string value)
    {
        if (passwordCaret != null)
        {
            passwordCaret.gameObject.SetActive(true);
            passwordCaret.enabled = true;
        }

        UpdateCaretPosition();
    }

    private void OnInputDeselected(string value)
    {
        if (passwordCaret != null)
        {
            passwordCaret.enabled = false;
        }
    }

    private void LateUpdate()
    {
        if (passwordCaret == null)
            return;

        bool focused = passwordInput.isFocused;

        if (!focused)
        {
            passwordCaret.enabled = false;
            passwordCaret.gameObject.SetActive(false);
            return;
        }

        if (!passwordCaret.gameObject.activeSelf)
        {
            passwordCaret.gameObject.SetActive(true);
        }

        UpdateCaretPosition();

        float blinkCycle = blinkInterval * 2f;

        bool shouldShowCaret =blinkCycle > 0f && (Time.unscaledTime % blinkCycle) < blinkInterval;

        passwordCaret.enabled = shouldShowCaret;
    }

    private void UpdateCaretPosition()
    {
        if (passwordCaret == null || passwordDisplay == null)
        {
            return;
        }

        passwordDisplay.ForceMeshUpdate();

        TMP_TextInfo textInfo = passwordDisplay.textInfo;

        int characterCount = textInfo.characterCount;
        int caretPosition = Mathf.Clamp(passwordInput.caretPosition, 0, characterCount);

        float caretX;
        float caretY;

        if (characterCount == 0)
        {
            caretX = 0f;
            caretY = 0f;
        }

        else if (caretPosition < characterCount)
        {
            TMP_CharacterInfo character = textInfo.characterInfo[caretPosition];

            caretX = character.bottomLeft.x;
            caretY = character.baseLine;
        }
        else
        {
            TMP_CharacterInfo character = textInfo.characterInfo[characterCount - 1];

            caretX = character.xAdvance;
            caretY = character.baseLine;
        }

        RectTransform caretRect = passwordCaret.rectTransform;

        caretRect.anchoredPosition = new Vector2(caretX, caretY);
    }
}
