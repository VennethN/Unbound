using UnityEngine;

public class PathMover : MonoBehaviour
{
    [Header("Path Points")]
    public Transform[] points;

    [Header("Movement Settings")]
    public float speed = 3f;
    public bool startMoving = false;

    [Header("Animation")]
    public Animator animator; // Drag your Animator here

    private int currentIndex = 0;

    private void Update()
    {
        // No movement? Play idle
        if (!startMoving || points.Length == 0)
        {
            SetWalking(false);
            return;
        }

        // Move towards point
        Vector3 target = points[currentIndex].position;
        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);

        // Switching animation when actually moving
        SetWalking(true);

        // Reached point?
        if (Vector3.Distance(transform.position, target) < 0.05f)
        {
            currentIndex++;

            // Finished all points
            if (currentIndex >= points.Length)
            {
                startMoving = false;
                SetWalking(false);  // stop animating
            }
        }
    }

    public void BeginMovement()
    {
        currentIndex = 0;
        startMoving = true;
    }

    private void SetWalking(bool walking)
    {
        if (animator != null)
        {
            animator.SetBool("isWalking", walking);
        }
    }
}
