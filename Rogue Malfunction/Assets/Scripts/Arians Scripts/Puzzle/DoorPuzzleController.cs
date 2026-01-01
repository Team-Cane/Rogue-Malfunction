using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // NEW

public class DoorPuzzleController : MonoBehaviour
{
    public enum ControlChangeType
    {
        None,
        SwapForwardBackward, // W <-> S
        SwapLeftRight        // A <-> D
    }

    [Header("Player & Interaction")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private AriansPlayerController playerController;
    [SerializeField] private float interactDistance = 3f;

    [Header("Puzzle Settings")]
    [Tooltip("The required sequence of keys (W/A/S/D) to solve this door.")]
    [SerializeField] private KeyCode[] requiredSequence;

    [Tooltip("Total time (in seconds) allowed to complete the entire sequence. <= 0 means no time limit.")]
    [SerializeField] private float totalTime = 3f;

    [Header("UI References")]
    [Tooltip("The root panel for the door puzzle UI.")]
    [SerializeField] private GameObject puzzlePanel;

    [Tooltip("Optional: icons for each step of the sequence (in order).")]
    [SerializeField] private Image[] stepIcons;

    [SerializeField] private Color defaultColor = Color.white;
    [SerializeField] private Color correctColor = Color.green;
    [SerializeField] private Color wrongColor = Color.red;

    [Header("Time UI (Optional)")]
    [Tooltip("Optional: Image used as a time bar (fillAmount goes from 1 → 0).")]
    [SerializeField] private Image timeBarImage;

    [Tooltip("Optional: TextMeshPro label showing numeric time remaining, e.g. 2.8")]
    [SerializeField] private TMP_Text timeRemainingText;   // CHANGED

    [Header("Fail Feedback")]
    [Tooltip("Full-screen or overlay image to flash red on failure.")]
    [SerializeField] private Image failFlashImage;
    [SerializeField] private float failFlashDuration = 0.5f;

    [Header("Door & Portal")]
    [Tooltip("Visual mesh of the door (will be disabled on success).")]
    [SerializeField] private GameObject doorVisual;

    [Tooltip("Collider that blocks the player (will be disabled on success).")]
    [SerializeField] private Collider doorCollider;

    [Tooltip("If not null, this portal will be enabled when the puzzle is solved.")]
    [SerializeField] private GameObject portalToEnable;

    [Header("Control Change")]
    [Tooltip("What control remap to apply when this door is successfully opened.")]
    [SerializeField] private ControlChangeType controlChangeOnOpen = ControlChangeType.None;

    private bool puzzleActive = false;
    private bool puzzleSolved = false;

    private int currentIndex = 0;
    private float puzzleTimer = 0f;
    private bool inFailFlash = false;

    private void Start()
    {
        if (puzzlePanel != null)
            puzzlePanel.SetActive(false);

        if (failFlashImage != null)
        {
            Color c = failFlashImage.color;
            c.a = 0f;
            failFlashImage.color = c;
        }

        ResetStepIcons();
        ResetTimeUI();

        if (doorCollider == null)
            doorCollider = GetComponent<Collider>();
        if (doorVisual == null)
            doorVisual = gameObject;
    }

    private void Update()
    {
        if (puzzleSolved)
            return;

        if (!puzzleActive)
            HandleDoorInteraction();
        else
            UpdatePuzzle();
    }

    private void HandleDoorInteraction()
    {
        if (playerTransform == null || playerController == null)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                if (hit.collider == doorCollider)
                {
                    float dist = Vector3.Distance(playerTransform.position, transform.position);
                    if (dist <= interactDistance)
                    {
                        OpenPuzzle();
                    }
                }
            }
        }
    }

    private void OpenPuzzle()
    {
        if (puzzleActive || puzzleSolved)
            return;

        puzzleActive = true;
        puzzleTimer = 0f;
        currentIndex = 0;

        if (puzzlePanel != null)
            puzzlePanel.SetActive(true);

        ResetStepIcons();
        ResetTimeUI();

        if (playerController != null)
            playerController.SetInputLocked(true);
    }

    private void ClosePuzzle()
    {
        puzzleActive = false;

        if (puzzlePanel != null)
            puzzlePanel.SetActive(false);

        if (playerController != null)
            playerController.SetInputLocked(false);

        ResetSequence();
    }

    private void UpdatePuzzle()
    {
        // timer
        if (totalTime > 0f)
        {
            puzzleTimer += Time.deltaTime;
            float remaining = Mathf.Max(0f, totalTime - puzzleTimer);
            UpdateTimeUI(remaining);

            if (remaining <= 0f)
            {
                TriggerFail();
                return;
            }
        }

        // cancel
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
        {
            ClosePuzzle();
            return;
        }

        if (!Input.anyKeyDown || inFailFlash)
            return;

        KeyCode pressed = KeyCode.None;

        if (Input.GetKeyDown(KeyCode.W)) pressed = KeyCode.W;
        else if (Input.GetKeyDown(KeyCode.A)) pressed = KeyCode.A;
        else if (Input.GetKeyDown(KeyCode.S)) pressed = KeyCode.S;
        else if (Input.GetKeyDown(KeyCode.D)) pressed = KeyCode.D;

        if (pressed == KeyCode.None)
            return;

        CheckInput(pressed);
    }

    private void CheckInput(KeyCode pressed)
    {
        if (requiredSequence == null || requiredSequence.Length == 0)
        {
            Debug.LogWarning($"{name}: No sequence set on DoorPuzzleController.");
            return;
        }

        if (currentIndex >= requiredSequence.Length)
            return;

        bool correct = (pressed == requiredSequence[currentIndex]);

        if (stepIcons != null && currentIndex < stepIcons.Length && stepIcons[currentIndex] != null)
        {
            stepIcons[currentIndex].color = correct ? correctColor : wrongColor;
        }

        if (correct)
        {
            currentIndex++;

            if (currentIndex >= requiredSequence.Length)
            {
                PuzzleSolved();
            }
        }
        else
        {
            TriggerFail();
        }
    }

    private void TriggerFail()
    {
        if (inFailFlash)
            return;

        StartCoroutine(FailFlashRoutine());
        ResetSequence();
    }

    private IEnumerator FailFlashRoutine()
    {
        inFailFlash = true;

        if (failFlashImage != null)
        {
            Color c = failFlashImage.color;
            c.a = 0.8f;
            failFlashImage.color = c;
        }

        float t = 0f;
        while (t < failFlashDuration)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        if (failFlashImage != null)
        {
            Color c = failFlashImage.color;
            c.a = 0f;
            failFlashImage.color = c;
        }

        inFailFlash = false;
    }

    private void ResetSequence()
    {
        currentIndex = 0;
        puzzleTimer = 0f;
        ResetStepIcons();
        ResetTimeUI();
    }

    private void ResetStepIcons()
    {
        if (stepIcons == null || stepIcons.Length == 0)
            return;

        for (int i = 0; i < stepIcons.Length; i++)
        {
            if (stepIcons[i] != null)
                stepIcons[i].color = defaultColor;
        }
    }

    private void ResetTimeUI()
    {
        if (timeBarImage != null)
        {
            if (totalTime > 0f)
                timeBarImage.fillAmount = 1f;
            else
                timeBarImage.fillAmount = 0f;
        }

        if (timeRemainingText != null)
        {
            if (totalTime > 0f)
                timeRemainingText.text = totalTime.ToString("0.0");
            else
                timeRemainingText.text = string.Empty;
        }
    }

    private void UpdateTimeUI(float timeRemaining)
    {
        if (timeBarImage != null && totalTime > 0f)
        {
            float t = Mathf.Clamp01(timeRemaining / totalTime);
            timeBarImage.fillAmount = t;
        }

        if (timeRemainingText != null)
        {
            if (totalTime > 0f)
                timeRemainingText.text = Mathf.Max(0f, timeRemaining).ToString("0.0");
            else
                timeRemainingText.text = string.Empty;
        }
    }

    private void PuzzleSolved()
    {
        puzzleSolved = true;
        puzzleActive = false;

        if (puzzlePanel != null)
            puzzlePanel.SetActive(false);

        if (playerController != null)
            playerController.SetInputLocked(false);

        if (doorCollider != null)
            doorCollider.enabled = false;

        if (doorVisual != null)
            doorVisual.SetActive(false);

        if (portalToEnable != null)
            portalToEnable.SetActive(true);

        ApplyControlChange();
    }

    private void ApplyControlChange()
    {
        if (controlChangeOnOpen == ControlChangeType.None)
            return;

        if (ControlRewireManager.Instance == null)
        {
            Debug.LogWarning($"{name}: No ControlRewireManager found, cannot apply control change.");
            return;
        }

        switch (controlChangeOnOpen)
        {
            case ControlChangeType.SwapForwardBackward:
                ControlRewireManager.Instance.SwapForwardBackward();
                break;
            case ControlChangeType.SwapLeftRight:
                ControlRewireManager.Instance.SwapLeftRight();
                break;
        }
    }
}
