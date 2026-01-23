using System.Collections.Generic;
using UnityEngine;

public static class TeleportService
{
    struct TeleportGate
    {
        public float untilTime;
        public int ignorePortalId;
    }

    static readonly Dictionary<int, TeleportGate> gates = new();

    public static bool CanUsePortal(Teleportable t, int portalId)
    {
        var id = t.GetInstanceID();

        if (!gates.TryGetValue(id, out var gate))
            return true;

        if (Time.time < gate.untilTime)
            return false;

        if (gate.ignorePortalId == portalId)
            return false;

        return true;
    }

    public static void Teleport(
        Teleportable t,
        int exitPortalId,
        Vector3 exitPosition,
        Quaternion exitRotation,
        bool rotateToExit,
        bool preserveVelocity,
        float gateSeconds)
    {
        var rb = t.Rigidbody;

        Vector3 velocity = preserveVelocity ? rb.linearVelocity : Vector3.zero;

        t.OnPreTeleport();

        rb.position = exitPosition;

        if (rotateToExit)
            rb.rotation = exitRotation;

        t.OnPostTeleport(velocity);

        gates[t.GetInstanceID()] = new TeleportGate
        {
            untilTime = Time.time + gateSeconds,
            ignorePortalId = exitPortalId
        };
    }
}
