using UnityEngine;

public class ControlRewireDebugTester : MonoBehaviour
{
    private void Update()
    {
        if (ControlRewireManager.Instance == null)
            return;

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            ControlRewireManager.Instance.SwapForwardBackward();
            Debug.Log("Debug: Swapped forward/backward (W <-> S).");
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            ControlRewireManager.Instance.SwapLeftRight();
            Debug.Log("Debug: Swapped left/right (A <-> D).");
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            ControlRewireManager.Instance.ResetToDefault();
            Debug.Log("Debug: Reset controls to default WASD.");
        }
    }
}
