using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class AccountCardUI : MonoBehaviour
{
    public TextMeshProUGUI usernameText;
    public Image borderImage;
    public Color normalColor = Color.white;
    public Color selectedColor = Color.yellow;

    private Button button;
    private AccountData accountData;
    private AccountCardMode cardMode;

    private float lastClickTime;
    private Coroutine pendingSingleClick;
    private const float doubleClickDelay = 0.25f;

    public void Setup(AccountData data, AccountCardMode mode = AccountCardMode.Normal)
    {
        accountData = data;

        cardMode = mode;

        usernameText.text = accountData.username;

        button = GetComponent<Button>();

        button.onClick.RemoveAllListeners();

        button.onClick.AddListener(OnClick);
    }


    public void OnClick()
    {
        AccountManager manager = AccountManager.Instance;

        // Student cards inside the teacher viewer.
        if (cardMode == AccountCardMode.TeacherStudentView)
        {
            manager.SelectTeacherStudent(this);
            return;
        }

        // Assignment mode takes priority over login and double-clicking.
        if (manager.IsAssigningStudents)
        {
            manager.SelectAccount(this);
            return;
        }

        float timeSinceLastClick = Time.time - lastClickTime;

        if (pendingSingleClick != null &&
            timeSinceLastClick <= doubleClickDelay)
        {
            StopCoroutine(pendingSingleClick);
            pendingSingleClick = null;
            lastClickTime = 0f;

            // Only logged-in teachers can open the teacher viewer.
            if (accountData.accountType == AccountType.Teacher &&
                CurrentAccount.ActiveAccount != null &&
                CurrentAccount.ActiveAccount.accountType == AccountType.Teacher)
            {
                manager.OpenTeacherInfo(this);
            }

            return;
        }

        lastClickTime = Time.time;
        pendingSingleClick = StartCoroutine(DelayedSingleClick());
    }

    private IEnumerator DelayedSingleClick()
    {
        yield return new WaitForSecondsRealtime(doubleClickDelay);

        pendingSingleClick = null;
        lastClickTime = 0f;

        // Normal account selection works regardless of login state.
        AccountManager.Instance.SelectAccount(this);
    }

    private void OnDisable()
    {
        if (pendingSingleClick != null)
        {
            StopCoroutine(pendingSingleClick);
            pendingSingleClick = null;
        }

        lastClickTime = 0f;
    }


    public AccountData GetAccountData()
    {
        return accountData;
    }

    public void SetSelected(bool selected)
    {
        borderImage.color = selected ? selectedColor : normalColor;
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
