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

    public void Setup(AccountData data)
    {
        accountData = data;

        usernameText.text = accountData.username;

        button = GetComponent<Button>();

        button.onClick.RemoveAllListeners();

        button.onClick.AddListener(OnClick);
    }

    public void OnClick()
    {
        AccountManager.Instance.SelectAccount(this);
        Debug.Log("Clicked");
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
