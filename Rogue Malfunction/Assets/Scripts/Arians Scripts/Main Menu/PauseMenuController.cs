using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class PauseMenuController : MonoBehaviour
{
    public static PauseMenuController Instance { get; private set; }

    [Header("Input")]
    [SerializeField] private KeyCode pauseKey = KeyCode.Escape;

    [Header("Scene Names")]
    [SerializeField] private string mainMenuSceneName = "Main Menu";

    [Header("Root UI")]
    [SerializeField] private Canvas rootCanvas;

    [Header("Panels")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject settingsPanel;

    [Header("Settings UI (Master only)")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private TMP_Text volumeLabel; // optional

    private bool isPaused = false;
    private bool isAllowedInThisScene = true;

    private CursorLockMode prevLockMode;
    private bool prevCursorVisible;

    private void Awake()
    {
        // Singleton + persist
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;

        // UI defaults
        if (rootCanvas != null) rootCanvas.enabled = true;
        if (pausePanel != null) pausePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);

        // Start unpaused (important if you drop this prefab into a random scene)
        ForceUnpause();

        // Initialize volume UI from AudioManager
        InitVolumeUI();

        // Apply scene rules for current scene
        ApplySceneRules(SceneManager.GetActiveScene().name);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Update()
    {
        if (!isAllowedInThisScene)
            return;

        if (Input.GetKeyDown(pauseKey))
        {
            // If pause is currently open, Escape closes pause.
            if (isPaused)
            {
                Resume();
                return;
            }

            // If something else modal is open (door puzzle), do nothing.
            if (UIModalLock.IsLocked)
                return;

            Pause();
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplySceneRules(scene.name);
    }

    private void ApplySceneRules(string sceneName)
    {
        // Disable pause UI in main menu scene so it can't fight main menu
        isAllowedInThisScene = (sceneName != mainMenuSceneName);

        if (!isAllowedInThisScene)
        {
            // Ensure we are unpaused and hidden
            ForceUnpause();
            HideAllPanels();

            if (rootCanvas != null)
                rootCanvas.enabled = false;

            return;
        }

        // Gameplay scenes
        if (rootCanvas != null)
            rootCanvas.enabled = true;

        HideAllPanels();

        // Refresh slider display (useful if volume changed in menu)
        InitVolumeUI();
    }

    // ---------------- Button Hooks ----------------

    public void OnResumeClicked()
    {
        Resume();
    }

    public void OnSettingsClicked()
    {
        if (!isAllowedInThisScene) return;

        if (pausePanel != null) pausePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    public void OnBackFromSettingsClicked()
    {
        if (!isAllowedInThisScene) return;

        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(true);
    }

    public void OnMainMenuClicked()
    {
        // Unpause before scene change
        ForceUnpause();
        HideAllPanels();

        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void OnQuitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void OnMasterVolumeSliderChanged(float value)
    {
        value = Mathf.Clamp01(value);

        if (AudioManager.Instance != null)
            AudioManager.Instance.SetMasterVolume(value);
        else
            AudioListener.volume = value;

        UpdateVolumeLabel(value);
    }

    // ---------------- Pause Core ----------------

    private void Pause()
    {
        if (isPaused) return;

        isPaused = true;

        // Take modal lock so door puzzles can't open while paused
        UIModalLock.Lock();

        SaveCursorState();
        SetCursorForMenu();

        Time.timeScale = 0f;

        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(true);
    }

    private void Resume()
    {
        if (!isPaused) return;

        isPaused = false;

        Time.timeScale = 1f;

        HideAllPanels();
        RestoreCursorState();

        // Release modal lock
        UIModalLock.Unlock();
    }

    private void ForceUnpause()
    {
        if (isPaused)
        {
            isPaused = false;
            Time.timeScale = 1f;
            RestoreCursorState();
            // lock count resets on scene load anyway, but we try to be clean:
            UIModalLock.Reset();
        }
        else
        {
            Time.timeScale = 1f;
        }
    }

    private void HideAllPanels()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    // ---------------- Cursor Helpers ----------------

    private void SaveCursorState()
    {
        prevLockMode = Cursor.lockState;
        prevCursorVisible = Cursor.visible;
    }

    private void RestoreCursorState()
    {
        Cursor.lockState = prevLockMode;
        Cursor.visible = prevCursorVisible;
    }

    private void SetCursorForMenu()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // ---------------- Volume UI ----------------

    private void InitVolumeUI()
    {
        float v = 1f;

        if (AudioManager.Instance != null)
            v = AudioManager.Instance.MasterVolume;
        else
            v = AudioListener.volume;

        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.minValue = 0f;
            masterVolumeSlider.maxValue = 1f;
            masterVolumeSlider.wholeNumbers = false;
            masterVolumeSlider.value = v;
        }

        UpdateVolumeLabel(v);
    }

    private void UpdateVolumeLabel(float value)
    {
        if (volumeLabel == null) return;

        int percent = Mathf.RoundToInt(Mathf.Clamp01(value) * 100f);
        volumeLabel.text = percent + "%";
    }
}
