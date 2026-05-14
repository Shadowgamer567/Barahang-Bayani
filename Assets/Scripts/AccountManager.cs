using JetBrains.Annotations;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

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
    public GameObject mainMenu;

    [Header("Teacher Monitoring")]
    public GameObject teacherInfoPanel;
    public GameObject studentInfoPanel;

    public GameObject assignStudentsButton;
    public GameObject confirmAssignButton;

    private bool assigningStudents = false;

    private AccountData assigningTeacher;
    private List<AccountData> pendingStudents = new();

    [Header("Teacher Info")]
    public TextMeshProUGUI teacherNameText;

    public Transform assignedStudentContainer;

    [Header("Student Info")]
    public TextMeshProUGUI completedLevelsText;

    public TextMeshProUGUI quizStatsText;

    public AccountRegistry registry = new();

    private string registryPath => Application.persistentDataPath + "/accounts.json";

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            DontDestroyOnLoad(gameObject);
        }

        else
        {
            Destroy(gameObject);
        }
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

        string saveFile = Application.persistentDataPath + "/progress_" + selectedAccount.id + ".json";

        if (File.Exists(saveFile))
        {
            File.Delete(saveFile);
        }

        registry.accounts.RemoveAll(a => a.id == selectedAccount.id);

        foreach (AccountData account in registry.accounts)
        {
            if (account.assignedStudentIDs != null)
            {
                account.assignedStudentIDs.Remove(selectedAccount.id);
            }
        }

        selectedAccount = null;
        selectedCard = null;

        SaveAccount();

        RefreshUI();
    }

    public void RefreshUI()
    {
        Debug.Log("RefreshUI Started");

        if (studentContainer == null)
        {
            Debug.LogError("Student Container NULL");
            return;
        }

        if (teacherContainer == null)
        {
            Debug.LogError("Teacher Container NULL");
            return;
        }

        if (accountCardPrefab == null)
        {
            Debug.LogError("Account Card Prefab NULL");
            return;
        }

        // Clear old cards
        for (int i = studentContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(studentContainer.GetChild(i).gameObject);
        }

        for (int i = teacherContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(teacherContainer.GetChild(i).gameObject);
        }

        Debug.Log("Cleared old cards");

        foreach (AccountData account in registry.accounts)
        {
            Transform parent =
                account.accountType == AccountType.Student
                ? studentContainer
                : teacherContainer;

            GameObject obj =
                Instantiate(accountCardPrefab, parent);

            obj.SetActive(true);

            Debug.Log("Instantiated card object");

            AccountCardUI card =
                obj.GetComponent<AccountCardUI>();

            if (card == null)
            {
                Debug.LogError("AccountCardUI component missing");
                continue;
            }

            card.Setup(account);

            Debug.Log("Spawned UI Card for: " + account.username);
        }

        Debug.Log("RefreshUI Finished");
    }

    public void SubmitUsername()
    {
        CreateAccount(pendingAccountType);

        createAccountPanel.SetActive(false);

        accountSelectPanel.SetActive(true);

        accountManagementPanel.SetActive(true);

        backButton.SetActive(true);

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
        AccountData clickedAccount = card.GetAccountData();

        if (assigningStudents)
        {
            if (clickedAccount.accountType != AccountType.Student)
            {
                return;
            }

            if (pendingStudents.Contains(clickedAccount))
            {
                pendingStudents.Remove(clickedAccount);

                card.SetSelected(false);
            }

            else
            {
                pendingStudents.Add(clickedAccount);

                card.SetSelected(true);
            }

                return;
        }

        if (selectedCard != null)
        {
            selectedCard.SetSelected(false);
        }

        selectedCard = card;

        selectedAccount = card.GetAccountData();

        CurrentAccount.ActiveAccount = selectedAccount;

        ProgressManager.Instance.SetActiveAccount(selectedAccount);

        selectedCard.SetSelected(true);

        Debug.Log("Selected Account: " + selectedAccount.username);
    }

    public void StartAssignStudents()
    {
        if (selectedAccount == null)
            return;

        if (selectedAccount.accountType != AccountType.Teacher)
            return;

        assigningStudents = true;

        assigningTeacher = selectedAccount;

        pendingStudents.Clear();

        assignStudentsButton.SetActive(false);

        confirmAssignButton.SetActive(true);

        Debug.Log("Assignment Mode Started");
    }

    public void ConfirmAssignStudents()
    {
        if (assigningTeacher == null)
            return;

        assigningTeacher.assignedStudentIDs.Clear();

        foreach (AccountData student in pendingStudents)
        {
            assigningTeacher.assignedStudentIDs.Add(student.id);
        }

        SaveAccount();

        assigningStudents = false;

        assigningTeacher = null;

        pendingStudents.Clear();

        assignStudentsButton.SetActive(true);

        confirmAssignButton.SetActive(false);

        Debug.Log("Students Assigned");
    }

    void HideAllPanels()
    {
        if (accountSelectPanel != null)
            accountSelectPanel.SetActive(false);

        if (accountManagementPanel != null)
            accountManagementPanel.SetActive(false);

        if (createAccountPanel != null)
            createAccountPanel.SetActive(false);

        if (teacherInfoPanel != null)
            teacherInfoPanel.SetActive(false);

        if (studentInfoPanel != null)
            studentInfoPanel.SetActive(false);
    }

    public void OpenAccountMenu()
    {
        HideAllPanels();

        accountButton.SetActive(false);

        accountSelectPanel.SetActive(true);

        accountManagementPanel.SetActive(true);

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

    public void OpenTeacherInfo(AccountCardUI card)
    {
        AccountData account = card.GetAccountData();

        if (account.accountType != AccountType.Teacher)
            return;

        HideAllPanels();

        teacherInfoPanel.SetActive(true);

        backButton.SetActive(true);

        teacherNameText.text = account.username;
        
        foreach (Transform child in assignedStudentContainer)
        {
            Destroy(child.gameObject);
        }

        foreach (string studentID in account.assignedStudentIDs)
        {
            AccountData student = registry.accounts.Find(a => a.id == studentID);

            if (student == null)
                continue;

            GameObject obj = Instantiate(accountCardPrefab, assignedStudentContainer);

            obj.SetActive(true);

            AccountCardUI ui = obj.GetComponent<AccountCardUI>();

            ui.Setup(student, AccountCardMode.TeacherStudentView);
        }
    }

    public void OpenStudentInfo(AccountData student)
    {
        studentInfoPanel.SetActive(true);

        string path = Application.persistentDataPath + "/progress_" + student.id + ".json";

        if (!System.IO.File.Exists(path))
        {
            return;
        }    

        string json = System.IO.File.ReadAllText(path);

        ProgressData data = JsonUtility.FromJson<ProgressData>(json);

        completedLevelsText.text = "Completed Levels:\n";

        foreach (string level in data.completedLevel)
        {
            completedLevelsText.text += level + "\n";
        }

        quizStatsText.text = "Multiple Choice: " + data.multipleChoiceCorrect + "/" + data.multipleChoiceTotal + "\n\n" + "Identification: " + data.identificationCorrect + "/" + data.identificationTotal + "\n\n" + "True/False: " + data.trueFalseCorrect + "/" + data.trueFalseTotal;
    }
    public void CloseAccountMenu()
    {
        accountButton.SetActive(true);

        accountSelectPanel.SetActive(false);

        accountManagementPanel.SetActive(false);

        createAccountPanel.SetActive(false);

        backButton.SetActive(false);

        teacherInfoPanel.SetActive(false);

        mainMenu.SetActive(true);
    }

    public void SelectTeacherStudent(AccountCardUI card)
    {
        OpenStudentInfo(card.GetAccountData());
    }

    public void ConnectUI(AccountUIReferences ui)
    {
        accountButton = ui.accountButton;
        backButton = ui.backButton;

        accountSelectPanel = ui.accountSelectPanel;
        accountManagementPanel = ui.accountManagementPanel;
        createAccountPanel = ui.createAccountPanel;

        teacherInfoPanel = ui.teacherInfoPanel;
        studentInfoPanel = ui.studentInfoPanel;

        mainMenu = ui.mainMenu;

        assignStudentsButton = ui.assignStudentsButton;
        confirmAssignButton = ui.confirmAssignButton;

        studentContainer = ui.studentContainer;
        teacherContainer = ui.teacherContainer;
        assignedStudentContainer = ui.assignedStudentContainer;

        usernameInputField = ui.usernameInputField;

        teacherNameText = ui.teacherNameText;
        completedLevelsText = ui.completedLevelsText;
        quizStatsText = ui.quizStatsText;

        LoadAccount();

        HideAllPanels();

        accountButton.SetActive(true);

        backButton.SetActive(false);

        RefreshUI();

        Debug.Log("UI Connected Successfully");
    }

    // Update is called once per frame
    void Update()
    {

    }
}
