using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(AudioSource))]
public class SoundHandlerScene : MonoBehaviour
{
    [SerializeField] private AudioClip sound;
    [SerializeField] private bool loop;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void OnEnable()
    {
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
    }

    private void Start()
    {
        if (sound == null)
        {
            return;
        }

        audioSource.clip = sound;
        audioSource.loop = loop;
        audioSource.Play();
    }

    private void OnDisable()
    {
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
    }

    private void OnDestroy()
    {
        if (loop && audioSource != null)
        {
            audioSource.Stop();
        }
    }

    private void OnActiveSceneChanged(Scene previousScene, Scene nextScene)
    {
        if (loop && audioSource != null)
        {
            audioSource.Stop();
        }
    }
}
