using UnityEngine;

public class DeathZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // Find PlayerRespawn on the root / parent
        var respawn = other.GetComponentInParent<PlayerRespawn>();
        if (respawn == null) return;

        // Trigger respawn
        respawn.Respawn();

        // Play a random death VO, if available
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayDeathSfx();
        }
    }
}
