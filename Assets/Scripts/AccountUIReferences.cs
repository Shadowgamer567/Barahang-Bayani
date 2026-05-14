using TMPro;
using UnityEngine;

public class AccountUIReferences : MonoBehaviour
{
    [Header("Panels")]
    public GameObject accountButton;
    public GameObject backButton;
    public GameObject accountSelectPanel;
    public GameObject accountManagementPanel;
    public GameObject createAccountPanel;
    public GameObject teacherInfoPanel;
    public GameObject studentInfoPanel;
    public GameObject mainMenu;

    [Header("Teacher Monitoring")]
    public GameObject assignStudentsButton;
    public GameObject confirmAssignButton;

    [Header("Containers")]
    public Transform studentContainer;
    public Transform teacherContainer;
    public Transform assignedStudentContainer;

    [Header("TMP")]
    public TMP_InputField usernameInputField;

    public TextMeshProUGUI teacherNameText;
    public TextMeshProUGUI completedLevelsText;
    public TextMeshProUGUI quizStatsText;

    void Start()
    {
        Debug.Log("AccountUIReference started");

        if (AccountManager.Instance != null)
        {
            Debug.Log("Connecting UI");
            AccountManager.Instance.ConnectUI(this);
        }
        else
        {
            Debug.LogError("AccountManager.Instance is Null");
        }

    }
}