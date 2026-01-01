using UnityEngine;

public class PlayerControlRandomizer : MonoBehaviour
{
    private bool swapAxes;
    private bool invertHorizontal;
    private bool invertVertical;
    private bool randomizeDirection;

    public Vector2 ProcessMovementInput(float h, float v)
    {
        if (randomizeDirection)
        {
            Vector2[] dirs =
            {
                Vector2.up,
                Vector2.down,
                Vector2.left,
                Vector2.right
            };

            return dirs[Random.Range(0, dirs.Length)];
        }

        if (swapAxes)
            (h, v) = (v, h);

        if (invertHorizontal)
            h *= -1f;

        if (invertVertical)
            v *= -1f;

        return new Vector2(h, v);
    }

    // 🔒 Permanently apply changes
    public void ApplyChange(
        bool swap,
        bool invertX,
        bool invertY,
        bool randomize)
    {
        if (swap) swapAxes = !swapAxes;
        if (invertX) invertHorizontal = !invertHorizontal;
        if (invertY) invertVertical = !invertVertical;

        if (randomize)
            randomizeDirection = true; // once enabled, stays enabled
    }
}
