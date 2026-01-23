using UnityEngine;
using UnityEngine.Events;

public class TilePuzzle : MonoBehaviour
{
    [Header("Tiles")]
    [SerializeField] private PuzzleTile[] tiles;

    [Header("Solved")]
    [SerializeField] private GameObject portalToEnable;
    [SerializeField] private UnityEvent onSolved;

    [Header("Fail Reset")]
    [SerializeField] private Transform checkpoint;
    [SerializeField] private bool resetPortalOnFail = true;

    public bool IsSolved { get; private set; }

    void Awake()
    {
        if (tiles == null || tiles.Length == 0)
            tiles = GetComponentsInChildren<PuzzleTile>(true);

        if (portalToEnable)
            portalToEnable.SetActive(false);
    }

    public void HandlePlayerStepped(PuzzleTile tile, Collider playerCollider)
    {
        if (IsSolved) return;

        if (tile.IsLit)
        {
            ResetPuzzleAndReturnPlayer(playerCollider);
            return;
        }

        tile.SetLit(true);

        if (AllRequiredTilesLit())
            Solve();
    }

    bool AllRequiredTilesLit()
    {
        for (int i = 0; i < tiles.Length; i++)
        {
            var t = tiles[i];
            if (!t) continue;
            if (!t.Required) continue;
            if (!t.IsLit) return false;
        }
        return true;
    }

    void Solve()
    {
        IsSolved = true;

        if (portalToEnable)
            portalToEnable.SetActive(true);

        onSolved?.Invoke();
    }

    void ResetPuzzleAndReturnPlayer(Collider playerCollider)
    {
        IsSolved = false;

        for (int i = 0; i < tiles.Length; i++)
            if (tiles[i])
                tiles[i].ResetToStart();

        if (resetPortalOnFail && portalToEnable)
            portalToEnable.SetActive(false);

        if (!checkpoint) return;

        var playerGO = playerCollider.gameObject;

        var cc = playerGO.GetComponent<CharacterController>();
        if (cc)
        {
            cc.enabled = false;
            playerGO.transform.SetPositionAndRotation(checkpoint.position, checkpoint.rotation);
            cc.enabled = true;
            return;
        }

        var rb = playerGO.GetComponent<Rigidbody>();
        if (rb)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.position = checkpoint.position;
            rb.rotation = checkpoint.rotation;
            return;
        }

        playerGO.transform.SetPositionAndRotation(checkpoint.position, checkpoint.rotation);
    }
}
