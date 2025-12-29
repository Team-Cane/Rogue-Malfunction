using UnityEngine;

public class DeathZone : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        var respawn = other.GetComponentInParent<PlayerRespawn>();
        if (respawn == null) return;

        respawn.Respawn();
    }
}
