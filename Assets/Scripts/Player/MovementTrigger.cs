using UnityEngine;

public class MovementTrigger : MonoBehaviour
{
    public PathMover mover;     // Drag the mover object here in Inspector
    public bool triggerOnce = false; // If true, trigger only once

    private bool hasTriggered = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasTriggered) return; // Prevent future triggers if already triggered

        if (other.CompareTag("Player"))
        {
            mover.BeginMovement();

            if (triggerOnce)
            {
                hasTriggered = true;
            }
        }
    }
}
