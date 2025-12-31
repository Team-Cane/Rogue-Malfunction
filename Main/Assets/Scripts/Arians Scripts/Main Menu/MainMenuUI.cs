using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MainMenuUI : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string playSceneName = "GameScene";

    [Header("Panels")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject creditsPanel;

    [Header("Audio Settings UI")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private TMP_Text volumeLabel;

    private void Awake()
    {
        if (mainPanel != null) mainPanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(false);

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
            masterVolumeSlider.value = initialVolume;
        }

        UpdateVolumeLabel(initialVolume);
    }

    // -------- Buttons: Main --------

    public void OnPlayClicked()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayGameMusic();
        }

        if (!string.IsNullOrEmpty(playSceneName))
        {
            SceneManager.LoadScene(playSceneName);
        }
        else
        {
            Debug.LogWarning("MainMenuUI: playSceneName is not set.");
        }
    }

    public void OnSettingsClicked()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    public void OnCreditsClicked()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(true);
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

    // -------- Buttons: Settings / Credits --------

    public void OnBackFromSettingsClicked()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(false);
        if (mainPanel != null) mainPanel.SetActive(true);
    }

    public void OnBackFromCreditsClicked()
    {
        if (creditsPanel != null) creditsPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (mainPanel != null) mainPanel.SetActive(true);
    }

    // -------- Audio UI --------

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
