using UnityEngine;

public class ControlRandomizerVolume : MonoBehaviour
{
    [Header("Permanent Control Mutation")]
    public bool swapHorizontalVertical = true;
    public bool invertHorizontal;
    public bool invertVertical;
    public bool randomizeDirections;

    private bool triggered;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;

        PlayerControlRandomizer r = other.GetComponent<PlayerControlRandomizer>();
        if (r == null) return;

        r.ApplyChange(
            swapHorizontalVertical,
            invertHorizontal,
            invertVertical,
            randomizeDirections
        );

        triggered = true;

        // Optional: disable volume visuals/collider after use
        GetComponent<Collider>().enabled = false;
    }
}
