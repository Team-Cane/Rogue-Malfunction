using UnityEngine;

public class PressurePlateCounter : MonoBehaviour
{
    [Header("Puzzle Goal")]
    [SerializeField] private int platesNeeded = 3;

    [Header("Reveal")]
    [SerializeField] private RevealOnSolved reveal;

    [SerializeField] private bool revealOnlyOnce = true;

    private int platesPressed;
    private bool revealed;

    public void NotifyPressed()
    {
        platesPressed++;
        CheckSolved();
    }

    public void NotifyReleased()
    {
        platesPressed = Mathf.Max(0, platesPressed - 1);

        if (!revealOnlyOnce)
            CheckSolved();
    }

    private void CheckSolved()
    {
        if (revealed && revealOnlyOnce) return;

        if (platesPressed >= platesNeeded)
        {
            if (reveal != null)
                reveal.Reveal();

            revealed = true;
        }
    }
}
