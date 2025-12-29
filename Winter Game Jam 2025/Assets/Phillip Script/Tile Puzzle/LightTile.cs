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

    public bool Required => required;
    public bool IsLit { get; private set; }

    void Awake()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;

        if (!targetRenderer)
            targetRenderer = GetComponentInChildren<Renderer>(true);

        SetLit(startLit);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!required) return;
        if (!other.CompareTag("Player")) return;

        SetLit(true);
        Debug.Log("Tile triggered by: " + other.name);

    }

    public void SetLit(bool lit)
    {
        IsLit = lit;

        if (targetRenderer && unlitMaterial && litMaterial)
            targetRenderer.sharedMaterial = IsLit ? litMaterial : unlitMaterial;
    }
}
