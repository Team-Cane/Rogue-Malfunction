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

    public static bool CanUsePortal(Teleportable t, int portalInstanceId)
    {
        var id = t.GetInstanceID();

        if (!gates.TryGetValue(id, out var gate))
            return true;

        if (Time.time < gate.untilTime)
            return false;

        if (gate.ignorePortalId == portalInstanceId)
            return false;

        return true;
    }

    public static void Teleport(
        Teleportable t,
        int exitPortalInstanceId,
        Vector3 exitPosition,
        Quaternion exitRotation,
        bool rotateToExit,
        float gateSeconds)
    {
        t.OnPreTeleport();

        var root = t.Root;
        root.position = exitPosition;

        if (rotateToExit)
            root.rotation = exitRotation;

        t.OnPostTeleport();

        gates[t.GetInstanceID()] = new TeleportGate
        {
            untilTime = Time.time + gateSeconds,
            ignorePortalId = exitPortalInstanceId
        };
    }
}
