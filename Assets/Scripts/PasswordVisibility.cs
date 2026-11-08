using System.Collections;
using TMPro;
using UnityEngine;

public class PasswordVisibility : MonoBehaviour
{
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private TMP_Text passwordDisplay;
    [SerializeField] private float revealDuration = 1f;

    private Coroutine hideCoroutine;

    private void OnEnable()
    {
        passwordInput.onValueChanged.AddListener(UpdatePassword);
        UpdatePassword(passwordInput.text);
    }

    private void OnDisable()
    {
        passwordInput.onValueChanged.RemoveListener(UpdatePassword);

        if (hideCoroutine != null)
            StopCoroutine(hideCoroutine);
    }

    private void UpdatePassword(string password)
    {
        if (hideCoroutine != null)
            StopCoroutine(hideCoroutine);

        if (string.IsNullOrEmpty(password))
        {
            passwordDisplay.text = "";
            return;
        }

        passwordDisplay.text =
            new string('•', password.Length - 1) +
            password[password.Length - 1];

        hideCoroutine = StartCoroutine(HideLastCharacter(password));
    }

    private IEnumerator HideLastCharacter(string password)
    {
        yield return new WaitForSecondsRealtime(revealDuration);

        passwordDisplay.text = new string('•', password.Length);

        hideCoroutine = null;
    }
}