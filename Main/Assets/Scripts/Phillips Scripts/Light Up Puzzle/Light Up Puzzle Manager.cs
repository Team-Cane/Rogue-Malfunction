using UnityEngine;
using UnityEngine.Events;

public class TilePuzzle : MonoBehaviour
{
    [SerializeField] private PuzzleTile[] tiles;
    [SerializeField] private UnityEvent onSolved;

    public bool IsSolved { get; private set; }

    void Awake()
    {
        if (tiles == null || tiles.Length == 0)
            tiles = GetComponentsInChildren<PuzzleTile>(true);

        Evaluate();
    }

    void Update()
    {
        if (IsSolved) return;
        Evaluate();
    }

    void Evaluate()
    {
        for (int i = 0; i < tiles.Length; i++)
        {
            var t = tiles[i];
            if (!t) continue;
            if (!t.Required) continue;
            if (!t.IsLit) return;
        }

        IsSolved = true;
        onSolved?.Invoke();
    }
}
