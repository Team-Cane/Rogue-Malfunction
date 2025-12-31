using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MainMenuUI : MonoBehaviour
{
    [Header("Scene")]
    [Tooltip("Name of the scene to load when Play is pressed.")]
    [SerializeField] private string playSceneName = "GameScene"; // Set this in Inspector

    [Header("Panels")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject settingsPanel;

    [Header("Audio Settings")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private TMP_Text volumeLabel; // optional

    private const string MasterVolumeKey = "MasterVolume";

    private void Awake()
    {
        // Make sure panels start in correct state
        if (mainPanel != null) mainPanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);

        // Load saved volume or default to 1
        float savedVolume = PlayerPrefs.GetFloat(MasterVolumeKey, 1f);

        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.value = savedVolume;
            UpdateMasterVolume(savedVolume);
        }
        else
        {
            AudioListener.volume = savedVolume;
        }
    }

    // ---------------- BUTTON HOOKS ----------------

    public void OnPlayClicked()
    {
        if (!string.IsNullOrEmpty(playSceneName))
        {
            SceneManager.LoadScene(playSceneName);
        }
        else
        {
            Debug.LogWarning("MainMenuUI: Play scene name is not set.");
        }
    }

    public void OnSettingsClicked()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    public void OnBackFromSettingsClicked()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (mainPanel != null) mainPanel.SetActive(true);
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

    // ---------------- AUDIO ----------------

    public void OnMasterVolumeSliderChanged(float value)
    {
        UpdateMasterVolume(value);
        PlayerPrefs.SetFloat(MasterVolumeKey, value);
        PlayerPrefs.Save();
    }

    private void UpdateMasterVolume(float value)
    {
        AudioListener.volume = value;

        if (volumeLabel != null)
        {
            int percent = Mathf.RoundToInt(value * 100f);
            volumeLabel.text = percent + "%";
        }
    }
}
