using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
        if (cardMode == AccountCardMode.TeacherStudentView)
        {
            AccountManager.Instance.SelectTeacherStudent(this);

            return;
        }

        float timeSinceLastClick = Time.time - lastClickTime;

        if (timeSinceLastClick <= doubleClickDelay)
        {
            if (accountData.accountType == AccountType.Teacher)
            {
                AccountManager.Instance.OpenTeacherInfo(this);
            }
        }
        else
        {
            AccountManager.Instance.SelectAccount(this);
        }

        if (timeSinceLastClick <= doubleClickDelay)
        {
            lastClickTime = 0f;
        }
        else
        {
            lastClickTime = Time.time;
        }
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
