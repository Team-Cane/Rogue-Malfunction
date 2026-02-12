using UnityEngine;

public class SideToSideMover : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Maximum distance from the starting X position.")]
    [SerializeField] private float amplitude = 2f;

    [Tooltip("How fast it moves back and forth.")]
    [SerializeField] private float frequency = 1f;

    [Tooltip("If true, movement is applied in local space (relative to parent).")]
    [SerializeField] private bool useLocalSpace = true;

    [Tooltip("Randomize the starting phase so duplicates don't all sync.")]
    [SerializeField] private bool randomizePhase = true;

    private float baseX;
    private float phaseOffset;

    private void Awake()
    {
        // Record the starting X position
        if (useLocalSpace)
            baseX = transform.localPosition.x;
        else
            baseX = transform.position.x;

        // Optional phase randomization so many obstacles don't move identically
        phaseOffset = randomizePhase ? Random.Range(0f, Mathf.PI * 2f) : 0f;
    }

    private void OnEnable()
    {
        // Re-randomize phase on reuse (for pooling later)
        if (randomizePhase)
            phaseOffset = Random.Range(0f, Mathf.PI * 2f);
    }

    private void Update()
    {
        float time = Time.time * frequency + phaseOffset;
        float offset = Mathf.Sin(time) * amplitude;

        if (useLocalSpace)
        {
            Vector3 localPos = transform.localPosition;
            localPos.x = baseX + offset;
            transform.localPosition = localPos;
        }
        else
        {
            Vector3 worldPos = transform.position;
            worldPos.x = baseX + offset;
            transform.position = worldPos;
        }
    }
}
