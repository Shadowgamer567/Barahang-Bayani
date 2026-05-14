using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class CutsceneManager : MonoBehaviour
{
    public GameObject cutsceneUI;
    public Transform leftSpawn;
    public Transform rightSpawn;
    public TextMeshProUGUI dialogueText;
    public GameObject namePanel;
    public GameObject mainUI;
    public TextMeshProUGUI nameText;

    private CutsceneData currentCutscene;
    private int currentIndex = 0;
    private float clickCooldown = 0.2f;
    private float lastClickTime = 0;

    public System.Action OnCutsceneFinished;

    public void PlayCutscene(CutsceneData data)
    {
        StartCoroutine(PlayCutsceneRoutine(data));
    }

    void ShowLine()
    {
        if (currentCutscene == null || currentCutscene.line.Count == 0)
        {
            Debug.LogError("Cutscene is empty or null");
            EndCutscene();
            return;
        }

        var line = currentCutscene.line[currentIndex];
        dialogueText.text = line.Dialogue;

        GameObject prefabToSpawn = null;

        //Name
        if (!string.IsNullOrEmpty(line.speakerName))
        {
            namePanel.SetActive(true);
            nameText.text = line.speakerName;
        }
        else if (line.speakerType == SpeakerType.Hero && line.hero != null)
        {
            namePanel.SetActive(true);
            nameText.text = line.hero.heroName;
        }
        else if (line.speakerType == SpeakerType.Enemy && line.enemy != null)
        {
            namePanel.SetActive(true);
            nameText.text = line.enemy.enemyName;
        }
        else
        {
            namePanel.SetActive(false);
        }

        //Prefabs
        if (line.speakerType == SpeakerType.Hero && line.hero != null)
        {
            prefabToSpawn = line.hero.prefab;
        }
        else if (line.speakerType == SpeakerType.Enemy && line.enemy != null)
        {
            prefabToSpawn = line.enemy.prefab;
        }

        if (line.speakerType == SpeakerType.Narrator)
        {
            ClearSpawn(leftSpawn);
            ClearSpawn(rightSpawn);
            return;
        }

        if (prefabToSpawn == null && line.speakerType != SpeakerType.Narrator)
        {
            Debug.LogWarning("No prefab found for speaker");
            return;
        }

        Transform parent = line.isLeftSide ? leftSpawn : rightSpawn;

        ClearSpawn(parent);

        GameObject obj = Instantiate(prefabToSpawn, parent);
        obj.transform.localPosition = Vector3.zero;

        // Always face forward toward the camera
        obj.transform.localRotation = Quaternion.Euler(0, 180, 0);
    }

    public void NextLine()
    {
        currentIndex++;

        if(currentIndex >= currentCutscene.line.Count)
        {
            EndCutscene();
            return;
        }

        ShowLine();
    }

    public void EndCutscene()
    {
        cutsceneUI.SetActive(false);
        mainUI.SetActive(true);
        Time.timeScale = 1f;

        OnCutsceneFinished?.Invoke();
    }

    void ClearSpawn(Transform parent)
    {
        foreach (Transform child in parent) 
        {
            Destroy(child.gameObject);
        }
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (cutsceneUI.activeSelf)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                if (Time.unscaledTime - lastClickTime > clickCooldown)
                {
                    lastClickTime = Time.unscaledTime;
                    NextLine();
                }
            }
        }
    }

    IEnumerator PlayCutsceneRoutine(CutsceneData data)
    {
        yield return new WaitForSecondsRealtime(0.5f);
        currentCutscene = data;
        currentIndex = 0;

        cutsceneUI.SetActive(true);
        mainUI.SetActive(false);
        Time.timeScale = 0f;

        ShowLine();
    }
}
