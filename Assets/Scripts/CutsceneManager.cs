using TMPro;
using UnityEngine;

public class CutsceneManager : MonoBehaviour
{
    public GameObject cutsceneUI;
    public Transform leftSpawn;
    public Transform rightSpawn;
    public TMPro.TextMeshProUGUI dialogueText;

    private CutsceneData currentCutscene;
    private int currentIndex = 0;
    private GameObject currentLeft;
    private GameObject currentRight;
    private float clickCooldown = 0.2f;
    private float lastClickTime = 0;

    public System.Action OnCutsceneFinished;

    public void PlayCutscene(CutsceneData data)
    {
        currentCutscene = data;
        currentIndex = 0;

        cutsceneUI.SetActive(true);
        Time.timeScale = 0f;

        ShowLine();
    }

    void ShowLine()
    {
        var line = currentCutscene.line[currentIndex];

        dialogueText.text = line.Dialogue;

        if (line.isLeftSide)
        {
            if (currentLeft != null)
            {
                Destroy(currentLeft);
                currentLeft = Instantiate(line.characterPrefab, leftSpawn);
            }    
        }

        else
        {
            if (currentRight != null)
            {
                Destroy(currentRight);
                currentRight = Instantiate(line.characterPrefab, rightSpawn);
            }
        }
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

    void EndCutscene()
    {
        cutsceneUI.SetActive(false);
        Time.timeScale = 1f;

        OnCutsceneFinished?.Invoke();
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
            if (Input.GetMouseButtonDown(0) && Time.unscaledTime - lastClickTime > clickCooldown)
            {
                lastClickTime = Time.unscaledTime;
                NextLine();
            }
        }
    }
}
