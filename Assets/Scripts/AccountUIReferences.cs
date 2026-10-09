using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
    public GameObject cardDeck;

    [Header("Teacher Monitoring")]
    public GameObject assignStudentsButton;
    public GameObject confirmAssignButton;

    [Header("Containers")]
    public Transform studentContainer;
    public Transform teacherContainer;
    public Transform assignedStudentContainer;

    [Header("TMP")]
    public TMP_InputField usernameInputField;
    public TMP_InputField passwordInputField;

    public TextMeshProUGUI teacherNameText;
    public TextMeshProUGUI completedLevelsText;
    public TextMeshProUGUI quizStatsText;

    [Header("Buttons")]
    public GameObject createStudentButton;

    public GameObject createTeacherButton;

    public GameObject submitUsernameButton;

    public GameObject deleteAccountButton;

    [Header("Login")]
    public GameObject loginPanel;
    public TMP_InputField loginPasswordInput;
    public TMP_Text loginUsernameText;
    public GameObject confirmLoginButton;
    public GameObject cancelLoginButton;

    [Header("ClassroomPanel")]
    public GameObject classroomPanel;
    public GameObject openClassroomSelectionButton;

    [Header("Classroom Selection")]
    public GameObject classroomSelectionPanel;
    public Transform classroomContainer;
    public GameObject classroomCardPrefab;
    public GameObject openClassroomCreationButton;
    public GameObject cancelClassroomSelectionButton;
    public TMP_Text currentClassroomNameText;

    [Header("Classroom Creation")]
    public GameObject classroomCreationPanel;
    public TMP_InputField classroomNameInput;
    public GameObject confirmCreationButton;
    public GameObject cancelCreationButton;

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

        if (FirestoreClassroomManager.Instance != null)
        {
            FirestoreClassroomManager.Instance.ConnectUI(this);
            FirestoreClassroomManager.Instance.ConnectClassroomButtons(this);
        }
        else
        {
            Debug.LogError("FirestoreClassroomManager.Instance is Null");
        }
    }
}