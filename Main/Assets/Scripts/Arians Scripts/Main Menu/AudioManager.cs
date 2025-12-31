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
    [SerializeField] private string mainMenuSceneName = "Main Menu";

    [SerializeField] private AudioClip mainMenuMusic;
    [SerializeField, Range(0f, 1f)] private float mainMenuMusicVolume = 0.8f;
    [SerializeField] private float mainMenuMusicPitch = 1f;

    [SerializeField] private AudioClip globalGameMusic;
    [SerializeField, Range(0f, 1f)] private float globalGameMusicVolume = 0.8f;
    [SerializeField] private float globalGameMusicPitch = 1f;

    [Header("Scene Audio Library (Random Loop Clips Per Scene)")]
    [SerializeField] private SceneAudioBank[] scenes;

    [Header("Universal Death SFX / VO")]
    [SerializeField] private AudioClip[] deathClips;
    [SerializeField, Range(0f, 1f)] private float deathVolume = 1f;
    [SerializeField] private Vector2 deathPitchRange = new Vector2(1f, 1f);

    [Header("Global Volumes (for Settings UI)")]
    [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float musicVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;

    private const string MasterKey = "Audio_Master";
    private const string MusicKey = "Audio_Music";
    private const string SfxKey = "Audio_SFX";

    public float MasterVolume => masterVolume;
    public float MusicVolume => musicVolume;
    public float SfxVolume => sfxVolume;

    Coroutine sceneLoopRoutine;

    [System.Serializable]
    public class SceneAudioBank
    {
        public string sceneName;

        [Header("Random Scene Loop Clips (VO / ambience)")]
        public AudioClip[] loopClips;
        [Range(0f, 1f)] public float loopVolume = 1f;
        public Vector2 loopPitchRange = new Vector2(1f, 1f);

        [Tooltip("If 0, it plays next clip immediately after current ends.")]
        public float gapBetweenClipsSeconds = 0f;

        [Tooltip("If true, waits one gap before the first clip plays.")]
        public bool delayFirst = false;
    }

    // ---------------- LIFECYCLE ----------------

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Ensure sources exist
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

        LoadVolumes();
        ApplyGlobalVolumes();

        SceneManager.sceneLoaded += OnSceneLoaded;

        // Apply for current scene (in case you start directly in a game scene)
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

    // ---------------- VOLUME PERSISTENCE ----------------

    void LoadVolumes()
    {
        masterVolume = PlayerPrefs.GetFloat(MasterKey, 1f);
        musicVolume = PlayerPrefs.GetFloat(MusicKey, 1f);
        sfxVolume = PlayerPrefs.GetFloat(SfxKey, 1f);
    }

    void SaveVolumes()
    {
        PlayerPrefs.SetFloat(MasterKey, masterVolume);
        PlayerPrefs.SetFloat(MusicKey, musicVolume);
        PlayerPrefs.SetFloat(SfxKey, sfxVolume);
        PlayerPrefs.Save();
    }

    void ApplyGlobalVolumes()
    {
        AudioListener.volume = masterVolume;

        if (musicSource != null)
            musicSource.volume = masterVolume * musicVolume * GetBaseMusicVolumeForCurrentClip();

        if (sceneLoopSource != null)
            sceneLoopSource.volume = masterVolume * sfxVolume * sceneLoopSource.volume; // gets overridden when loops start

        if (sfxSource != null)
            sfxSource.volume = masterVolume * sfxVolume;
    }

    float GetBaseMusicVolumeForCurrentClip()
    {
        if (musicSource.clip == mainMenuMusic)
            return mainMenuMusicVolume;
        if (musicSource.clip == globalGameMusic)
            return globalGameMusicVolume;
        return 1f;
    }

    // Public setters used by your Settings UI
    public void SetMasterVolume(float value)
    {
        masterVolume = Mathf.Clamp01(value);
        ApplyGlobalVolumes();
        SaveVolumes();
    }

    public void SetMusicVolume(float value)
    {
        musicVolume = Mathf.Clamp01(value);
        ApplyGlobalVolumes();
        SaveVolumes();
    }

    public void SetSfxVolume(float value)
    {
        sfxVolume = Mathf.Clamp01(value);
        ApplyGlobalVolumes();
        SaveVolumes();
    }

    // ---------------- SCENE AUDIO ----------------

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

    void ApplyMusicNoRestart(AudioClip clip, float baseVolume, float pitch)
    {
        if (!clip)
        {
            StopMusic();
            return;
        }

        bool needsClipSwap = musicSource.clip != clip;

        musicSource.pitch = Mathf.Clamp(pitch, -3f, 3f);
        musicSource.loop = true;

        // effective volume = baseVolume * master * music bus
        musicSource.volume = Mathf.Clamp01(baseVolume) * masterVolume * musicVolume;

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

                // effective volume = loopVolume * master * sfx
                sceneLoopSource.volume = Mathf.Clamp01(bank.loopVolume) * masterVolume * sfxVolume;

                sceneLoopSource.clip = clip;
                sceneLoopSource.Play();

                float length = clip.length / Mathf.Max(0.01f, Mathf.Abs(sceneLoopSource.pitch));
                yield return new WaitForSeconds(length);
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

    // ---------------- PUBLIC API ----------------

    public void StopMusic()
    {
        musicSource.Stop();
        musicSource.clip = null;
    }

    // Keep these so your MainMenuUI code still compiles,
    // even though music already follows scenes automatically.
    public void PlayMainMenuMusic()
    {
        ApplyMusic(mainMenuSceneName);
    }

    public void PlayGameMusic()
    {
        // Any scene name that isn't the main menu will select game music
        ApplyMusic("GameScene");
    }

    public void PlayDeathSfx()
    {
        var clip = PickRandom(deathClips);
        if (!clip) return;

        sfxSource.pitch = Mathf.Clamp(Random.Range(deathPitchRange.x, deathPitchRange.y), -3f, 3f);

        float effectiveVolume = Mathf.Clamp01(deathVolume) * masterVolume * sfxVolume;
        sfxSource.PlayOneShot(clip, effectiveVolume);
    }

    public void PlaySfx(AudioClip clip, float volumeMultiplier = 1f)
    {
        if (!clip) return;

        float effectiveVolume = Mathf.Clamp01(volumeMultiplier) * masterVolume * sfxVolume;
        sfxSource.PlayOneShot(clip, effectiveVolume);
    }

    // ---------------- UTIL ----------------

    static AudioClip PickRandom(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0) return null;
        return clips[Random.Range(0, clips.Length)];
    }
}
