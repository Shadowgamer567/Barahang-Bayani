using UnityEngine;

public class CardSound : MonoBehaviour
{
    [Header("Card Sounds")]
    [SerializeField] private AudioClip handRefilledSound;
    [SerializeField] private AudioClip cardPlayedSound;
    [SerializeField] private AudioClip cardClickedSound;

    private BattleControl battleControl;

    private void OnEnable()
    {
        CardPanelManager.OnHandRefilled += PlayHandRefilledSound;
        CardUI.OnCardClicked += PlayCardClickedSound;

        battleControl = FindFirstObjectByType<BattleControl>();
        if (battleControl != null)
        {
            battleControl.OnCardPlayAccepted += PlayCardPlayedSound;
        }
    }

    private void OnDisable()
    {
        CardPanelManager.OnHandRefilled -= PlayHandRefilledSound;
        CardUI.OnCardClicked -= PlayCardClickedSound;

        if (battleControl != null)
        {
            battleControl.OnCardPlayAccepted -= PlayCardPlayedSound;
            battleControl = null;
        }
    }

    private void PlayHandRefilledSound()
    {
        PlaySound(handRefilledSound);
    }

    private void PlayCardPlayedSound()
    {
        PlaySound(cardPlayedSound);
    }

    private void PlayCardClickedSound()
    {
        PlaySound(cardClickedSound);
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip == null)
        {
            return;
        }

        GameObject playbackObject = new GameObject("Card Sound");
        AudioSource playbackSource = playbackObject.AddComponent<AudioSource>();
        playbackSource.spatialBlend = 0f;
        SoundVolumeControl.RegisterSource(playbackSource, AudioVolumeGroup.Sfx);
        playbackSource.PlayOneShot(clip);
        DontDestroyOnLoad(playbackObject);
        Destroy(playbackObject, clip.length);
    }
}
