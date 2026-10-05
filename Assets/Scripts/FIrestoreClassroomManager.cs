using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using Firebase.Firestore;
using TMPro;

public class FirestoreClassroomManager : MonoBehaviour
{
    public static FirestoreClassroomManager Instance;

    [Header("Classroom UI")]
    public GameObject classroomSelectionPanel;
    public Transform classroomContainer;
    public GameObject classroomCardPrefab;
    public TMP_Text currentClassroomText;

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

    public async Task CreateClassroom(string classroomID)
    {
        if (string.IsNullOrWhiteSpace(classroomID))
        {
            Debug.LogError("Classroom ID cannot be empty.");
            return;
        }

        classroomID = classroomID.Trim();

        Dictionary<string, object> data =
            new Dictionary<string, object>();

        data["created"] = true;

        await FirestoreManager.Instance.DB
            .Collection("Classroom")
            .Document(classroomID)
            .SetAsync(data);

        Debug.Log("Classroom created: " + classroomID);
    }

    public async void LoadClassrooms()
    {
        if (classroomContainer == null)
        {
            Debug.LogError("Classroom container is not assigned.");
            return;
        }

        if (classroomCardPrefab == null)
        {
            Debug.LogError("Classroom card prefab is not assigned.");
            return;
        }

        QuerySnapshot snapshot = await FirestoreManager.Instance.DB
            .Collection("Classroom")
            .GetSnapshotAsync();

        // Clear existing classroom cards
        for (int i = classroomContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(classroomContainer.GetChild(i).gameObject);
        }

        foreach (DocumentSnapshot document in snapshot.Documents)
        {
            GameObject obj =
                Instantiate(classroomCardPrefab, classroomContainer);

            obj.SetActive(true);

            ClassroomCardUI card =
                obj.GetComponent<ClassroomCardUI>();

            if (card == null)
            {
                Debug.LogError(
                    "ClassroomCard prefab is missing ClassroomCardUI."
                );

                continue;
            }

            card.Setup(document.Id);
        }

        Debug.Log(
            "Loaded " + snapshot.Count + " classrooms from Firestore."
        );
    }

    public void OpenClassroomSelection()
    {
        if (classroomSelectionPanel == null)
            return;

        classroomSelectionPanel.SetActive(true);

        LoadClassrooms();
    }

    public void CloseClassroomSelection()
    {
        if (classroomSelectionPanel == null)
            return;

        classroomSelectionPanel.SetActive(false);
    }

    public async void SelectClassroom(string classroomID)
    {
        if (string.IsNullOrWhiteSpace(classroomID))
        {
            return;
        }

        Debug.Log("Selecting Classroom: " + classroomID);

        DatabaseManager.Instance.SetDatabase(classroomID);

        if (currentClassroomText != null)
        {
            currentClassroomText.text = classroomID;
        }

        CloseClassroomSelection();

        await FirestoreAccountManager.Instance.DownloadAccounts();

        // Refresh account UI
        if (AccountManager.Instance != null)
        {
            AccountManager.Instance.RefreshUI();
        }

        Debug.Log("Classroom switched successfully: " + classroomID);
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        if (currentClassroomText != null &&
            DatabaseManager.Instance != null)
        {
            currentClassroomText.text =
                DatabaseManager.Instance.CurrentDatabase;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
