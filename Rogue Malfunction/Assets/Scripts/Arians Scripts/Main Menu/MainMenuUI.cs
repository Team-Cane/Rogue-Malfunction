using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class MainMenuUI : MonoBehaviour
{
    [Header("Play Flow")]
    [Tooltip("If true, Play will load playSceneName directly instead of opening Level Select. Useful for quick testing.")]
    [SerializeField] private bool debugPlayDirectly = false;

    [Tooltip("Used only if Debug Play Directly is enabled.")]
    [SerializeField] private string playSceneName = "GameScene";

    [Header("Panels")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject levelSelectionPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject creditsPanel;

    [Header("Optional: Default Selected UI (Controller/Keyboard Navigation)")]
    [SerializeField] private GameObject mainFirstSelected;
    [SerializeField] private GameObject levelSelectFirstSelected;
    [SerializeField] private GameObject settingsFirstSelected;
    [SerializeField] private GameObject creditsFirstSelected;

    [Header("Audio Settings UI")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private TMP_Text volumeLabel;

    private enum MenuState
    {
        Main,
        LevelSelect,
        Settings,
        Credits
    }

    private void Awake()
    {
        // Safety: never let menu load while paused
        Time.timeScale = 1f;

        // IMPORTANT: reset remapped controls whenever we hit the main menu
        if (ControlRewireManager.Instance != null)
        {
            ControlRewireManager.Instance.ResetToDefault();
        }

        // Ensure panel state
        Show(MenuState.Main);

        float initialVolume = 1f;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMainMenuMusic();
            initialVolume = AudioManager.Instance.MasterVolume;
        }
        else
        {
            initialVolume = AudioListener.volume;
        }

        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.SetValueWithoutNotify(initialVolume);
        }

        UpdateVolumeLabel(initialVolume);
    }

    // ---------------- Panel Navigation ----------------

    private void Show(MenuState state)
    {
        if (mainPanel != null) mainPanel.SetActive(state == MenuState.Main);
        if (levelSelectionPanel != null) levelSelectionPanel.SetActive(state == MenuState.LevelSelect);
        if (settingsPanel != null) settingsPanel.SetActive(state == MenuState.Settings);
        if (creditsPanel != null) creditsPanel.SetActive(state == MenuState.Credits);

        SetSelectedForState(state);
    }

    private void SetSelectedForState(MenuState state)
    {
        if (EventSystem.current == null) return;

        GameObject toSelect = null;

        switch (state)
        {
            case MenuState.Main:
                toSelect = mainFirstSelected;
                break;
            case MenuState.LevelSelect:
                toSelect = levelSelectFirstSelected;
                break;
            case MenuState.Settings:
                toSelect = settingsFirstSelected;
                break;
            case MenuState.Credits:
                toSelect = creditsFirstSelected;
                break;
        }

        if (toSelect != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(toSelect);
        }
    }

    // ---------------- Buttons: Main ----------------

    public void OnPlayClicked()
    {
        if (debugPlayDirectly)
        {
            // Quick test path
            if (!string.IsNullOrEmpty(playSceneName))
            {
                PlayLevel(playSceneName);
            }
            else
            {
                Debug.LogWarning("MainMenuUI: playSceneName is not set (Debug Play Directly is enabled).");
            }

            return;
        }

        // Normal path: open Level Select panel
        Show(MenuState.LevelSelect);
    }

    public void OnSettingsClicked()
    {
        Show(MenuState.Settings);
    }

    public void OnCreditsClicked()
    {
        Show(MenuState.Credits);
    }

    public void OnQuitClicked()
    {
        Debug.Log("MainMenuUI: Quit requested.");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ---------------- Buttons: Back ----------------

    public void OnBackFromSettingsClicked()
    {
        Show(MenuState.Main);
    }

    public void OnBackFromCreditsClicked()
    {
        Show(MenuState.Main);
    }

    public void OnBackFromLevelSelectClicked()
    {
        Show(MenuState.Main);
    }

    // ---------------- Level Select ----------------

    /// <summary>
    /// Hook every level button to this and pass the scene name as the argument.
    /// Example scene names you said: "Sequencer", "The 3 B's", "Sokoban", "Tiler"
    /// </summary>
    public void PlayLevel(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("MainMenuUI: PlayLevel called with an empty sceneName.");
            return;
        }

        // Helpful validation: prevents silent failures when scene isn't in Build Settings
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"MainMenuUI: Scene \"{sceneName}\" cannot be loaded. " +
                           "Make sure the scene is added to Build Settings and the name matches exactly.");
            return;
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayGameMusic();
        }

        SceneManager.LoadScene(sceneName);
    }

    // ---------------- Audio UI ----------------

    public void OnMasterVolumeSliderChanged(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMasterVolume(value);
        }
        else
        {
            AudioListener.volume = Mathf.Clamp01(value);
        }

        UpdateVolumeLabel(value);
    }

    private void UpdateVolumeLabel(float value)
    {
        if (volumeLabel == null) return;

        int percent = Mathf.RoundToInt(Mathf.Clamp01(value) * 100f);
        volumeLabel.text = percent + "%";
    }
}
