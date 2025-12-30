using UnityEngine;
using UnityEngine.Events;

public class TilePuzzle : MonoBehaviour
{
    [Header("Tiles")]
    [SerializeField] private PuzzleTile[] tiles;

    [Header("Solved")]
    [SerializeField] private GameObject portalToEnable;
    [SerializeField] private UnityEvent onSolved;

    public bool IsSolved { get; private set; }

    void Awake()
    {
        if (tiles == null || tiles.Length == 0)
            tiles = GetComponentsInChildren<PuzzleTile>(true);

        if (portalToEnable)
            portalToEnable.SetActive(false);
    }

    void Update()
    {
        if (IsSolved) return;

        for (int i = 0; i < tiles.Length; i++)
        {
            var tile = tiles[i];
            if (!tile) continue;
            if (!tile.Required) continue;
            if (!tile.IsLit) return;
        }

        Solve();
    }

    void Solve()
    {
        IsSolved = true;

        if (portalToEnable)
            portalToEnable.SetActive(true);

        onSolved?.Invoke();
    }
}
