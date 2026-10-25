using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum AudioVolumeGroup
{
    Music,
    Sfx
}

[RequireComponent(typeof(Slider))]
public class VolumeHandler : MonoBehaviour
{
    [SerializeField] private AudioVolumeGroup volumeGroup;

    private Slider volumeSlider;

    private void Awake()
    {
        volumeSlider = GetComponent<Slider>();
    }

    private void OnEnable()
    {
        volumeSlider.onValueChanged.AddListener(ApplyVolume);
        float volume = SoundVolumeControl.GetVolume(volumeGroup);
        float sliderValue = Mathf.Lerp(volumeSlider.minValue, volumeSlider.maxValue, volume);
        volumeSlider.SetValueWithoutNotify(sliderValue);
        ApplyVolume(sliderValue);
    }

    private void OnDisable()
    {
        volumeSlider.onValueChanged.RemoveListener(ApplyVolume);
        SoundVolumeControl.SaveVolumes();
    }

    private void ApplyVolume(float value)
    {
        float volume = Mathf.InverseLerp(volumeSlider.minValue, volumeSlider.maxValue, value);
        SoundVolumeControl.SetVolume(volumeGroup, volume);
    }
}

public static class SoundVolumeControl
{
    private sealed class RegisteredSource
    {
        public AudioVolumeGroup Group;
        public float BaseVolume;
    }

    private static readonly Dictionary<AudioSource, RegisteredSource> Sources =
        new Dictionary<AudioSource, RegisteredSource>();

    private static readonly Dictionary<AudioVolumeGroup, float> Volumes =
        new Dictionary<AudioVolumeGroup, float>
        {
            { AudioVolumeGroup.Music, PlayerPrefs.GetFloat("MusicVolume", 1f) },
            { AudioVolumeGroup.Sfx, PlayerPrefs.GetFloat("SfxVolume", 1f) }
        };

    public static float GetVolume(AudioVolumeGroup group)
    {
        return Volumes[group];
    }

    public static void SetVolume(AudioVolumeGroup group, float volume)
    {
        volume = Mathf.Clamp01(volume);
        Volumes[group] = volume;
        PlayerPrefs.SetFloat(GetPreferenceKey(group), volume);

        List<AudioSource> destroyedSources = null;
        foreach (KeyValuePair<AudioSource, RegisteredSource> entry in Sources)
        {
            if (entry.Key == null)
            {
                destroyedSources ??= new List<AudioSource>();
                destroyedSources.Add(entry.Key);
                continue;
            }

            if (entry.Value.Group == group)
            {
                entry.Key.volume = entry.Value.BaseVolume * volume;
            }
        }

        if (destroyedSources != null)
        {
            foreach (AudioSource source in destroyedSources)
            {
                Sources.Remove(source);
            }
        }
    }

    public static void SaveVolumes()
    {
        PlayerPrefs.Save();
    }

    private static string GetPreferenceKey(AudioVolumeGroup group)
    {
        return group == AudioVolumeGroup.Music ? "MusicVolume" : "SfxVolume";
    }

    public static void RegisterSource(AudioSource source, AudioVolumeGroup group, float baseVolume = 1f)
    {
        if (source == null)
        {
            Debug.LogError("Cannot register a missing AudioSource for volume control.");
            return;
        }

        Sources[source] = new RegisteredSource
        {
            Group = group,
            BaseVolume = baseVolume
        };

        source.volume = baseVolume * Volumes[group];
    }
}
