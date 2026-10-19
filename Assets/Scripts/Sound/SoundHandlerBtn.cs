using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
[RequireComponent(typeof(AudioSource))]
public class SoundHandlerBtn : MonoBehaviour
{
    [SerializeField] private AudioClip clickSound;

    private Button button;
    private AudioSource audioSource;

    private void Awake()
    {
        button = GetComponent<Button>();
        audioSource = GetComponent<AudioSource>();
        button.onClick.AddListener(PlayClickSound);
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(PlayClickSound);
        }
    }

    private void PlayClickSound()
    {
        if (clickSound != null)
        {
            audioSource.PlayOneShot(clickSound);
        }
    }
}
