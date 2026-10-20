using UnityEngine;

public class BattleSound : MonoBehaviour
{
    [Header("Combat Events")]
    [SerializeField] private AudioClip hpLostSound;
    [SerializeField, Range(0f, 2f)] private float hpLostVolume = 1f;
    [SerializeField] private AudioClip shieldGainedSound;
    [SerializeField, Range(0f, 2f)] private float shieldGainedVolume = 1f;
    [SerializeField] private AudioClip healedSound;
    [SerializeField, Range(0f, 2f)] private float healedVolume = 1f;

    [Header("Character Animations")]
    [SerializeField] private AudioClip deathSound;
    [SerializeField, Range(0f, 2f)] private float deathVolume = 1f;
    [SerializeField] private AudioClip actionSound;
    [SerializeField, Range(0f, 2f)] private float actionVolume = 1f;
    [SerializeField] private AudioClip hitSound;
    [SerializeField, Range(0f, 2f)] private float hitVolume = 1f;
    [SerializeField] private AudioClip attackSound;
    [SerializeField, Range(0f, 2f)] private float attackVolume = 1f;

    private int lastHpLostSoundFrame = -1;
    private int lastDeathSoundFrame = -1;
    private int lastHitSoundFrame = -1;

    private void OnEnable()
    {
        HeroStats.OnHpLost += PlayHpLostSound;
        EnemyStats.OnHpLost += PlayHpLostSound;
        HeroStats.OnShieldGained += PlayShieldGainedSound;
        EnemyStats.OnShieldGained += PlayShieldGainedSound;
        HeroStats.OnHealed += PlayHealedSound;
        EnemyStats.OnHealed += PlayHealedSound;
        CharacterAnimationRelay.OnAnimationPlayed += PlayAnimationSound;
    }

    private void OnDisable()
    {
        HeroStats.OnHpLost -= PlayHpLostSound;
        EnemyStats.OnHpLost -= PlayHpLostSound;
        HeroStats.OnShieldGained -= PlayShieldGainedSound;
        EnemyStats.OnShieldGained -= PlayShieldGainedSound;
        HeroStats.OnHealed -= PlayHealedSound;
        EnemyStats.OnHealed -= PlayHealedSound;
        CharacterAnimationRelay.OnAnimationPlayed -= PlayAnimationSound;
    }

    private void PlayHpLostSound()
    {
        if (lastHpLostSoundFrame == Time.frameCount)
        {
            return;
        }

        lastHpLostSoundFrame = Time.frameCount;
        PlaySound(hpLostSound, hpLostVolume);
    }

    private void PlayShieldGainedSound()
    {
        PlaySound(shieldGainedSound, shieldGainedVolume);
    }

    private void PlayHealedSound()
    {
        PlaySound(healedSound, healedVolume);
    }

    private void PlayAnimationSound(CharacterAnimationType animationType)
    {
        switch (animationType)
        {
            case CharacterAnimationType.Death:
                if (lastDeathSoundFrame == Time.frameCount)
                {
                    break;
                }

                lastDeathSoundFrame = Time.frameCount;
                PlaySound(deathSound, deathVolume);
                break;
            case CharacterAnimationType.Action:
                PlaySound(actionSound, actionVolume);
                break;
            case CharacterAnimationType.Damaged:
                if (lastHitSoundFrame == Time.frameCount)
                {
                    break;
                }

                lastHitSoundFrame = Time.frameCount;
                PlaySound(hitSound, hitVolume);
                break;
            case CharacterAnimationType.Attack:
                PlaySound(attackSound, attackVolume);
                break;
        }
    }

    private void PlaySound(AudioClip clip, float volume)
    {
        if (clip == null)
        {
            return;
        }

        GameObject playbackObject = new GameObject("Battle Sound");
        AudioSource playbackSource = playbackObject.AddComponent<AudioSource>();
        playbackSource.spatialBlend = 0f;
        playbackSource.volume = volume;
        playbackSource.PlayOneShot(clip);
        Destroy(playbackObject, clip.length);
    }
}
