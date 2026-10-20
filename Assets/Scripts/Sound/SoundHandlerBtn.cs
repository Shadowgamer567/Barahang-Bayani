using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SoundHandlerBtn : MonoBehaviour
{
    [SerializeField] private AudioClip clickSound;

    private readonly HashSet<Button> registeredButtons = new HashSet<Button>();

    private void OnEnable()
    {
        InvokeRepeating(nameof(RefreshButtonListeners), 0.1f, 0.25f);
    }

    private void OnDisable()
    {
        CancelInvoke(nameof(RefreshButtonListeners));

        foreach (Button button in registeredButtons)
        {
            if (button != null)
            {
                button.onClick.RemoveListener(PlayClickSound);
            }
        }

        registeredButtons.Clear();
    }

    private void RefreshButtonListeners()
    {
        Button[] buttons = FindObjectsByType<Button>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (Button button in buttons)
        {
            if (button == null)
            {
                continue;
            }

            button.onClick.RemoveListener(PlayClickSound);
            button.onClick.AddListener(PlayClickSound);
            registeredButtons.Add(button);
        }

        registeredButtons.RemoveWhere(button => button == null);
    }

    private void PlayClickSound()
    {
        if (clickSound == null)
        {
            return;
        }

        GameObject playbackObject = new GameObject("UI Click Sound");
        AudioSource playbackSource = playbackObject.AddComponent<AudioSource>();
        playbackSource.spatialBlend = 0f;
        playbackSource.PlayOneShot(clickSound);
        DontDestroyOnLoad(playbackObject);
        Destroy(playbackObject, clickSound.length);
    }
}
