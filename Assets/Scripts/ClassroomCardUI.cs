using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ClassroomCardUI : MonoBehaviour
{
    public TMP_Text classroomNameText;

    private string classroomID;


    public void Setup(string classroomID)
    {
        this.classroomID = classroomID;

        classroomNameText.text = classroomID;

        Button button = GetComponent<Button>();

        button.onClick.RemoveAllListeners();

        button.onClick.AddListener(SelectClassroom);
    }

    private void SelectClassroom()
    {
        if (FirestoreClassroomManager.Instance != null)
        {
            FirestoreClassroomManager.Instance.SelectClassroom(classroomID);
        }
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
