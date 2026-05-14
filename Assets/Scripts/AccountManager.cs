using JetBrains.Annotations;
using System.IO;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class AccountManager : MonoBehaviour
{
    public Transform studentContainer;
    public Transform teacherContainer;

    public AccountCardUI selectedCard;

    public AccountData selectedAccount;

    public GameObject accountCardPrefab;

    public TMP_InputField usernameInputField;

    private AccountType pendingAccountType;

    public static AccountManager Instance;

    [Header("Panels")]
    public GameObject accountButton;
    public GameObject backButton;
    public GameObject accountSelectPanel;
    public GameObject accountManagementPanel;
    public GameObject createAccountPanel;

    public AccountRegistry registry = new();

    private string registryPath => Application.persistentDataPath + "/accounts.json";

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    private void Awake()
    {
        Instance = this;

        LoadAccount();

        accountSelectPanel.SetActive(false);

        accountManagementPanel.SetActive(false);

        createAccountPanel.SetActive(false);

        accountButton.SetActive(true);
    }

    public void LoadAccount()
    {
        if (!File.Exists(registryPath))
        {
            SaveAccount();
            return;
        }

        string json = File.ReadAllText(registryPath);

        registry = JsonUtility.FromJson<AccountRegistry>(json);

        if (registry == null)
        {
            registry = new AccountRegistry();
        }

        RefreshUI();
    }

    public void SaveAccount()
    {
        string json = JsonUtility.ToJson(registry, true);

        File.WriteAllText(registryPath, json);
    }
    public void CreateStudentAccount()
    {
        CreateAccount(AccountType.Student);
    }

    public void CreateTeacherAccount()
    {
        CreateAccount(AccountType.Teacher);
    }

    public void CreateAccount(AccountType type)
    {
        string username = usernameInputField.text.Trim();

        if (string.IsNullOrEmpty(username))
        {
            Debug.Log("UsernameEmpty");
            return;
        }

        AccountData newAccount = new AccountData();

        newAccount.id = System.Guid.NewGuid().ToString();

        newAccount.username = username;

        newAccount.accountType = type;

        registry.accounts.Add(newAccount);

        SaveAccount();

        Debug.Log("Created Account");

        usernameInputField.text = "";

        RefreshUI();
    }

    public void DeleteSelected()
    {
        if (selectedAccount == null)
            return;

        registry.accounts.Remove(selectedAccount);

        selectedAccount = null;
        selectedCard = null;

        SaveAccount();

        RefreshUI();
    }

    public void RefreshUI()
    {
        foreach (Transform child in studentContainer)
        {
            if (child.gameObject != accountCardPrefab)
            {
                Destroy(child.gameObject);
            }
        }

        foreach (Transform child in teacherContainer)
        {
            if (child.gameObject != accountCardPrefab)
            {
                Destroy(child.gameObject);
            }
        }

        foreach (AccountData account in registry.accounts)
        {
            Transform parent = account.accountType == AccountType.Student ? studentContainer : teacherContainer;

            GameObject obj = Instantiate(accountCardPrefab, parent);

            obj.SetActive(true);

            AccountCardUI card = obj.GetComponent<AccountCardUI>();

            card.Setup(account);

            Debug.Log("Spawned UI Card for: " + account.username);
        }
    }

    public void SubmitUsername()
    {
        CreateAccount(pendingAccountType);

        createAccountPanel.SetActive(false);

        accountSelectPanel.SetActive(true);

        accountManagementPanel.SetActive(true);

        RefreshUI();
    }

    private void ReIndexAccounts()
    {
        for (int i = 0; i < registry.accounts.Count; i++)
        {
            registry.accounts[i].id = i.ToString();
        }
    }

    public void SelectAccount(AccountCardUI card)
    { 
        if (selectedCard != null)
        {
            selectedCard.SetSelected(false);
        }

        selectedCard = card;

        selectedAccount = card.GetAccountData();

        CurrentAccount.ActiveAccount = selectedAccount;

        selectedCard.SetSelected(true);

        Debug.Log("Selected Account: " + selectedAccount.username);
    }

    public void OpenAccountMenu()
    {
        accountButton.SetActive(false);

        accountSelectPanel.SetActive(true);

        accountManagementPanel.SetActive(true);

        createAccountPanel.SetActive(false);

        backButton.SetActive(true);
    }

    public void OpenCreateStudentMenu()
    {
        pendingAccountType = AccountType.Student;

        accountSelectPanel.SetActive(false);

        accountManagementPanel.SetActive(false);

        createAccountPanel.SetActive(true);

        backButton.SetActive(false);
    }

    public void OpenCreateTeacherMenu()
    {
        pendingAccountType = AccountType.Teacher;

        accountSelectPanel.SetActive(false);

        accountManagementPanel.SetActive(false);

        createAccountPanel.SetActive(true);

        backButton.SetActive(false);
    }

    public void CloseAccountMenu()
    {
        accountButton.SetActive(true);

        accountSelectPanel.SetActive(false);

        accountManagementPanel.SetActive(false);

        createAccountPanel.SetActive(false);

        backButton.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {

        }
    }
}
