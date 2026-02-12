using UnityEngine;

public class ControlRewireManager : MonoBehaviour
{
    // Simple singleton so other scripts can grab the current manager.
    public static ControlRewireManager Instance { get; private set; }

    [Header("Current Key Mapping (read-only in play mode)")]
    [SerializeField] private KeyCode upKey = KeyCode.W;
    [SerializeField] private KeyCode downKey = KeyCode.S;
    [SerializeField] private KeyCode leftKey = KeyCode.A;
    [SerializeField] private KeyCode rightKey = KeyCode.D;

    public KeyCode UpKey => upKey;
    public KeyCode DownKey => downKey;
    public KeyCode LeftKey => leftKey;
    public KeyCode RightKey => rightKey;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // Optional: remove if you want per-scene managers

        ResetToDefault();
    }

    public void ResetToDefault()
    {
        upKey = KeyCode.W;
        downKey = KeyCode.S;
        leftKey = KeyCode.A;
        rightKey = KeyCode.D;
    }

    /// <summary>
    /// Door 1 effect: swap forward/backward (W <-> S).
    /// </summary>
    public void SwapForwardBackward()
    {
        KeyCode temp = upKey;
        upKey = downKey;
        downKey = temp;
    }

    /// <summary>
    /// Door 2 effect: swap left/right (A <-> D).
    /// </summary>
    public void SwapLeftRight()
    {
        KeyCode temp = leftKey;
        leftKey = rightKey;
        rightKey = temp;
    }

    /// <summary>
    /// Convenience method if you ever want to set a mapping explicitly.
    /// </summary>
    public void SetMapping(KeyCode newUp, KeyCode newDown, KeyCode newLeft, KeyCode newRight)
    {
        upKey = newUp;
        downKey = newDown;
        leftKey = newLeft;
        rightKey = newRight;
    }
}
