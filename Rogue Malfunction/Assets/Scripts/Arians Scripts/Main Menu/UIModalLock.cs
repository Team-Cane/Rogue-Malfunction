using UnityEngine;
using UnityEngine.SceneManagement;

public static class UIModalLock
{
    // If > 0, some modal UI is open (door puzzle, pause menu, etc.)
    private static int lockCount = 0;

    public static bool IsLocked => lockCount > 0;

    static UIModalLock()
    {
        // Reset lock on every scene load to avoid "stuck locked" state.
        SceneManager.sceneLoaded += (_, __) => Reset();
    }

    public static void Lock()
    {
        lockCount++;
        if (lockCount < 0) lockCount = 0;
    }

    public static void Unlock()
    {
        lockCount--;
        if (lockCount < 0) lockCount = 0;
    }

    public static void Reset()
    {
        lockCount = 0;
    }
}
