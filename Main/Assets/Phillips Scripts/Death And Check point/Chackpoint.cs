using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        var respawn = other.GetComponentInParent<PlayerRespawn>();
        if (respawn == null) return;

        respawn.SetCheckpoint(transform);
    }
}
