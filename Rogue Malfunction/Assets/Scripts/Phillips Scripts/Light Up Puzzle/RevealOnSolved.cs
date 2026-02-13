using UnityEngine;

public class RevealOnSolved : MonoBehaviour
{
    [SerializeField] private GameObject target;

    public void Reveal()
    {
        if (target)
            target.SetActive(true);
    }
}
