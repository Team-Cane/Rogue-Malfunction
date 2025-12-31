using System.Collections;
using UnityEngine;

public class SceneAudioController : MonoBehaviour
{
    [Header("Music")]
    [SerializeField] private bool playGameMusicOnStart = true;

    [Header("Timed Voice Lines")]
    [Tooltip("Voice lines to be played in this scene over time.")]
    [SerializeField] private AudioClip[] timedVoiceLines;

    [Tooltip("Seconds to wait before the first VO line.")]
    [SerializeField] private float firstDelay = 5f;

    [Tooltip("Seconds between VO lines.")]
    [SerializeField] private float intervalBetweenLines = 20f;

    [Tooltip("If true, loops back to the first line after the last.")]
    [SerializeField] private bool loopLines = false;

    private Coroutine voRoutine;

    private void Start()
    {
        if (AudioManager.Instance == null)
        {
            Debug.LogWarning("SceneAudioController: No AudioManager found in scene.");
            return;
        }

        if (playGameMusicOnStart)
        {
            AudioManager.Instance.PlayGameMusic();
        }

        if (timedVoiceLines != null && timedVoiceLines.Length > 0)
        {
            voRoutine = StartCoroutine(PlayVoiceLinesRoutine());
        }
    }

    private IEnumerator PlayVoiceLinesRoutine()
    {
        yield return new WaitForSeconds(firstDelay);

        int index = 0;
        while (true)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayDeathSfx();
            }

            index++;

            if (index >= timedVoiceLines.Length)
            {
                if (loopLines)
                {
                    index = 0;
                }
                else
                {
                    yield break;
                }
            }

            yield return new WaitForSeconds(intervalBetweenLines);
        }
    }
}
