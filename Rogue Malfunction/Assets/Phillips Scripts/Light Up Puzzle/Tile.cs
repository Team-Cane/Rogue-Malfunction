using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PuzzleTile : MonoBehaviour
{
    [SerializeField] private bool required = true;
    [SerializeField] private bool startLit;

    [Header("Visual")]
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private Material unlitMaterial;
    [SerializeField] private Material litMaterial;

    TilePuzzle puzzle;

    public bool Required => required;
    public bool IsLit { get; private set; }

    void Awake()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;

        if (!targetRenderer)
            targetRenderer = GetComponentInChildren<Renderer>(true);

        puzzle = GetComponentInParent<TilePuzzle>();

        ResetToStart();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!required) return;
        if (!other.CompareTag("Player")) return;

        if (puzzle)
            puzzle.HandlePlayerStepped(this, other);
        else
            SetLit(true);
    }

    public void ResetToStart()
    {
        SetLit(startLit);
    }

    public void SetLit(bool lit)
    {
        IsLit = lit;

        if (targetRenderer && unlitMaterial && litMaterial)
            targetRenderer.sharedMaterial = IsLit ? litMaterial : unlitMaterial;
    }
}
