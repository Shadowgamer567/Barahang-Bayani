using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class CutsceneManager : MonoBehaviour
{
    public GameObject cutsceneUI;
    public Transform leftSpawn;
    public Transform rightSpawn;

    [Header("Cutscene Cameras")]
    public Camera heroCamera;
    public Camera enemyCamera;

    [Header("Cutscene Animation")]
    public RuntimeAnimatorController cutsceneAnimatorController;

    public TextMeshProUGUI dialogueText;
    public GameObject namePanel;
    public GameObject mainUI;
    public TextMeshProUGUI nameText;

    private CutsceneData currentCutscene;
    private int currentIndex = 0;
    private float clickCooldown = 0.2f;
    private float lastClickTime = 0;

    private GameObject leftCharacter;
    private GameObject rightCharacter;

    private GameObject leftCharacterPrefab;
    private GameObject rightCharacterPrefab;

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
            ClearCutsceneCharacters();
            return;
        }

        if (prefabToSpawn == null && line.speakerType != SpeakerType.Narrator)
        {
            Debug.LogWarning("No prefab found for speaker");
            return;
        }

        // Select the camera for the character's display side.
        Camera cutsceneCamera = line.isLeftSide ? heroCamera : enemyCamera;

        // Spawn or reuse the character.
        GameObject obj = GetOrCreateCharacter(prefabToSpawn, line.isLeftSide, cutsceneCamera);

        // Automatically frame the spawned character.
        FrameCharacter(obj, cutsceneCamera);

        // Play the pose assigned to this dialogue line.
        Animator characterAnimator = obj.GetComponentInChildren<Animator>();

        if (characterAnimator != null)
        {
            PlayCutscenePose(characterAnimator, line.pose);
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

    private void PlayCutscenePose(Animator animator, CutscenePose pose)
    {
        if (animator == null)
            return;

        string stateName = "Cutscene" + pose.ToString();

        int stateHash = Animator.StringToHash(stateName);

        // Fall back to Idle if the selected pose is unavailable.
        if (!animator.HasState(0, stateHash))
        {
            stateHash = Animator.StringToHash("CutsceneIdle");
        }

        animator.Play(stateHash, 0, 0f);
    }


    private void FrameCharacter(GameObject character, Camera camera)
    {
        Debug.Log(
        $"FRAME CHARACTER CALLED | " +
        $"Character: {(character != null ? character.name : "NULL")} | " +
        $"Camera: {(camera != null ? camera.name : "NULL")}"
    );

        if (character == null || camera == null)
        {
            return;
        }

        Renderer[] renderers = character.GetComponentsInChildren<Renderer>();

        bool hasBounds = false;
        Bounds characterBounds = new Bounds();

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || !renderer.enabled)
            {
                continue;
            }

            if (!hasBounds)
            {
                characterBounds = renderer.bounds;
                hasBounds = true;
            }

            else
            {
                characterBounds.Encapsulate(renderer.bounds);
            }
        }

        if (!hasBounds)
        {
            Debug.LogWarning("Cannot frame character: No enabled renderers found.");
            return;
        }

        // Calculate the camera distance needed to fit the character.
        float verticalHalfAngle = camera.fieldOfView * 0.5f * Mathf.Deg2Rad;

        float horizontalHalfAngle = Mathf.Atan(Mathf.Tan(verticalHalfAngle) * camera.aspect);

        float verticalDistance = characterBounds.extents.y / Mathf.Tan(verticalHalfAngle);

        float horizontalDistance = characterBounds.extents.x / Mathf.Tan(horizontalHalfAngle);

        // Add some breathing room around the character.
        float padding = 1.15f;

        float distance = Mathf.Max(verticalDistance, horizontalDistance) * padding ;

        distance *= 0.9f;

        // Center the camera on the character's bounds.
        Vector3 center = characterBounds.center;

        camera.transform.position = center - camera.transform.forward * distance;

        Debug.Log(
    $"FRAMING RESULT | Bounds: {characterBounds.size} | " +
    $"Distance: {distance} | " +
    $"New Camera Position: {camera.transform.position}"
);
    }


    public void EndCutscene()
    {
        ClearCutsceneCharacters();

        cutsceneUI.SetActive(false);
        mainUI.SetActive(true);
        Time.timeScale = 1f;

        OnCutsceneFinished?.Invoke();
    }

    private GameObject GetOrCreateCharacter(GameObject prefab, bool isLeftSide, Camera cutsceneCamera)
    {
        Transform spawn = isLeftSide ? leftSpawn : rightSpawn;

        GameObject currentCharacter = isLeftSide ? leftCharacter : rightCharacter;

        GameObject currentPrefab = isLeftSide ? leftCharacterPrefab : rightCharacterPrefab;

        // Reuse the existing character if it matches the requested prefab.
        if (currentCharacter != null && currentPrefab == prefab)
        {
            return currentCharacter;
        }

        // Remove the previous character from this side.
        if (currentCharacter != null)
        {
            Destroy(currentCharacter);
        }

        // Spawn the new character.
        GameObject obj = Instantiate(prefab, spawn);

        obj.transform.localPosition = Vector3.zero;

        // Face the appropriate cutscene camera.
        if (cutsceneCamera != null)
        {
            Vector3 direction = cutsceneCamera.transform.position - obj.transform.position;

            direction.y = 0f;

            if (direction.sqrMagnitude > 0.001f)
            {
                obj.transform.rotation = Quaternion.LookRotation(direction);
            }
        }

        // Apply the shared cutscene Animator.
        Animator[] animators = obj.GetComponentsInChildren<Animator>();

        foreach (Animator animator in animators)
        {
            if (cutsceneAnimatorController != null)
            {
                animator.runtimeAnimatorController = cutsceneAnimatorController;
            }

            animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            animator.applyRootMotion = false;
        }

        // Remember the spawned character.
        if (isLeftSide)
        {
            leftCharacter = obj;
            leftCharacterPrefab = prefab;
        }
        else
        {
            rightCharacter = obj;
            rightCharacterPrefab = prefab;
        }

        return obj;
    }

    void ClearSpawn(Transform parent)
    {
        foreach (Transform child in parent) 
        {
            Destroy(child.gameObject);
        }
    }

    private void ClearCutsceneCharacters()
    {
        ClearSpawn(leftSpawn);
        ClearSpawn(rightSpawn);

        leftCharacter = null;
        rightCharacter = null;

        leftCharacterPrefab = null;
        rightCharacterPrefab = null;
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
        ClearCutsceneCharacters();

        cutsceneUI.SetActive(true);
        mainUI.SetActive(false);
        Time.timeScale = 0f;

        ShowLine();
    }
}
