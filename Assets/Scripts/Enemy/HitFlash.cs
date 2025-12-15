using UnityEngine;

public class HitFlash : MonoBehaviour
{
    [Header("Flash Settings")]
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private float flashDuration = 0.1f;

    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private bool isFlashing = false;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
    }

    public void Flash()
    {
        if (!isFlashing)
            StartCoroutine(FlashRoutine());
    }

    private System.Collections.IEnumerator FlashRoutine()
    {
        isFlashing = true;

        // Change sprite to flash color
        spriteRenderer.color = flashColor;

        // Wait
        yield return new WaitForSeconds(flashDuration);

        // Return to original
        spriteRenderer.color = originalColor;

        isFlashing = false;
    }
}
