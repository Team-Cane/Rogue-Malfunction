using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sceneLoopSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Mixer Routing (Optional)")]
    [SerializeField] private AudioMixerGroup musicGroup;
    [SerializeField] private AudioMixerGroup sceneLoopGroup;
    [SerializeField] private AudioMixerGroup sfxGroup;

    [Header("Music")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [SerializeField] private AudioClip mainMenuMusic;
    [SerializeField, Range(0f, 1f)] private float mainMenuMusicVolume = 0.8f;
    [SerializeField] private float mainMenuMusicPitch = 1f;

    [SerializeField] private AudioClip globalGameMusic;
    [SerializeField, Range(0f, 1f)] private float globalGameMusicVolume = 0.8f;
    [SerializeField] private float globalGameMusicPitch = 1f;

    [Header("Scene Audio Library (Random Loop Clips Per Scene)")]
    [SerializeField] private SceneAudioBank[] scenes;

    [Header("Universal Death SFX")]
    [SerializeField] private AudioClip[] deathClips;
    [SerializeField, Range(0f, 1f)] private float deathVolume = 1f;
    [SerializeField] private Vector2 deathPitchRange = new Vector2(1f, 1f);

    Coroutine sceneLoopRoutine;

    [System.Serializable]
    public class SceneAudioBank
    {
        public string sceneName;

        [Header("Random Scene Loop Clips")]
        public AudioClip[] loopClips;
        [Range(0f, 1f)] public float loopVolume = 1f;
        public Vector2 loopPitchRange = new Vector2(1f, 1f);

        [Tooltip("If 0, it plays next clip immediately after current ends.")]
        public float gapBetweenClipsSeconds = 0f;

        [Tooltip("If true, waits one gap before the first clip plays.")]
        public bool delayFirst = false;
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (!musicSource) musicSource = gameObject.AddComponent<AudioSource>();
        if (!sceneLoopSource) sceneLoopSource = gameObject.AddComponent<AudioSource>();
        if (!sfxSource) sfxSource = gameObject.AddComponent<AudioSource>();

        musicSource.playOnAwake = false;
        musicSource.loop = true;

        sceneLoopSource.playOnAwake = false;
        sceneLoopSource.loop = false;

        sfxSource.playOnAwake = false;
        sfxSource.loop = false;

        if (musicGroup) musicSource.outputAudioMixerGroup = musicGroup;
        if (sceneLoopGroup) sceneLoopSource.outputAudioMixerGroup = sceneLoopGroup;
        if (sfxGroup) sfxSource.outputAudioMixerGroup = sfxGroup;

        SceneManager.sceneLoaded += OnSceneLoaded;

        ApplyForScene(SceneManager.GetActiveScene().name);
    }

    void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyForScene(scene.name);
    }

    void ApplyForScene(string sceneName)
    {
        ApplyMusic(sceneName);
        ApplySceneLoop(sceneName);
    }

    void ApplyMusic(string sceneName)
    {
        if (sceneName == mainMenuSceneName)
        {
            ApplyMusicNoRestart(mainMenuMusic, mainMenuMusicVolume, mainMenuMusicPitch);
            return;
        }

        ApplyMusicNoRestart(globalGameMusic, globalGameMusicVolume, globalGameMusicPitch);
    }

    void ApplyMusicNoRestart(AudioClip clip, float volume, float pitch)
    {
        if (!clip)
        {
            StopMusic();
            return;
        }

        bool needsClipSwap = musicSource.clip != clip;

        musicSource.volume = Mathf.Clamp01(volume);
        musicSource.pitch = Mathf.Clamp(pitch, -3f, 3f);
        musicSource.loop = true;

        if (needsClipSwap)
        {
            musicSource.clip = clip;
            musicSource.Play();
            return;
        }

        if (!musicSource.isPlaying)
            musicSource.Play();
    }

    void ApplySceneLoop(string sceneName)
    {
        StopSceneLoop();

        var bank = GetBank(sceneName);
        if (bank == null) return;

        if (bank.loopClips != null && bank.loopClips.Length > 0)
            sceneLoopRoutine = StartCoroutine(SceneLoop(bank));
    }

    IEnumerator SceneLoop(SceneAudioBank bank)
    {
        if (bank.delayFirst && bank.gapBetweenClipsSeconds > 0f)
            yield return new WaitForSeconds(bank.gapBetweenClipsSeconds);

        while (true)
        {
            var clip = PickRandom(bank.loopClips);
            if (clip)
            {
                sceneLoopSource.pitch = Mathf.Clamp(Random.Range(bank.loopPitchRange.x, bank.loopPitchRange.y), -3f, 3f);
                sceneLoopSource.volume = Mathf.Clamp01(bank.loopVolume);
                sceneLoopSource.clip = clip;
                sceneLoopSource.Play();

                yield return new WaitForSeconds(clip.length / Mathf.Max(0.01f, Mathf.Abs(sceneLoopSource.pitch)));
            }
            else
            {
                yield return null;
            }

            if (bank.gapBetweenClipsSeconds > 0f)
                yield return new WaitForSeconds(bank.gapBetweenClipsSeconds);
        }
    }

    void StopSceneLoop()
    {
        if (sceneLoopRoutine != null)
        {
            StopCoroutine(sceneLoopRoutine);
            sceneLoopRoutine = null;
        }

        sceneLoopSource.Stop();
        sceneLoopSource.clip = null;
    }

    SceneAudioBank GetBank(string sceneName)
    {
        if (scenes == null) return null;

        for (int i = 0; i < scenes.Length; i++)
        {
            if (scenes[i] != null && scenes[i].sceneName == sceneName)
                return scenes[i];
        }

        return null;
    }

    public void StopMusic()
    {
        musicSource.Stop();
        musicSource.clip = null;
    }

    public void PlayDeathSfx()
    {
        var clip = PickRandom(deathClips);
        if (!clip) return;

        sfxSource.pitch = Mathf.Clamp(Random.Range(deathPitchRange.x, deathPitchRange.y), -3f, 3f);
        sfxSource.PlayOneShot(clip, Mathf.Clamp01(deathVolume));
    }

    static AudioClip PickRandom(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0) return null;
        return clips[Random.Range(0, clips.Length)];
    }
}
