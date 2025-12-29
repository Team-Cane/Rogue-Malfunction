using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider))]
public class ScenePortal : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string sceneName;

    [Header("Rules")]
    [SerializeField] private float triggerGateSeconds = 0.75f;

    [Header("Filtering")]
    [SerializeField] private LayerMask validLayers = ~0;

    static bool isLoading;
    float nextAllowedTime;

    void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (isLoading)
            return;

        if (Time.time < nextAllowedTime)
            return;

        if (((1 << other.gameObject.layer) & validLayers) == 0)
            return;

        if (!other.CompareTag("Player"))
            return;

        if (string.IsNullOrEmpty(sceneName))
            return;

        isLoading = true;
        nextAllowedTime = Time.time + triggerGateSeconds;

        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }
}
