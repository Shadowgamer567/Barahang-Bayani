using UnityEngine;

public class AnswerSound : MonoBehaviour
{
    [SerializeField] private AudioClip correctAnswerSound;
    [SerializeField] private AudioClip incorrectAnswerSound;

    private QuizManager quizManager;

    private void OnEnable()
    {
        quizManager = GetComponent<QuizManager>();
        if (quizManager != null)
        {
            quizManager.OnAnswerEvaluated += PlayAnswerSound;
        }
    }

    private void OnDisable()
    {
        if (quizManager != null)
        {
            quizManager.OnAnswerEvaluated -= PlayAnswerSound;
            quizManager = null;
        }
    }

    private void PlayAnswerSound(bool isCorrect)
    {
        AudioClip clip = isCorrect ? correctAnswerSound : incorrectAnswerSound;
        if (clip == null)
        {
            return;
        }

        GameObject playbackObject = new GameObject("Answer Sound");
        AudioSource playbackSource = playbackObject.AddComponent<AudioSource>();
        playbackSource.spatialBlend = 0f;
        SoundVolumeControl.RegisterSource(playbackSource, AudioVolumeGroup.Sfx);
        playbackSource.PlayOneShot(clip);
        Destroy(playbackObject, clip.length);
    }
}
