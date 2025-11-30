using UnityEngine;

public class MovementTrigger : MonoBehaviour
{
    public PathMover mover;  // Drag the mover object here in Inspector

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            mover.BeginMovement();
        }
    }
}
