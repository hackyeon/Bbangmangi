using System.Collections;
using UnityEngine;

public class HitFlash : MonoBehaviour
{
    public Color flashColor = Color.white;
    public float flashDuration = 0.08f;

    private Renderer bodyRenderer;
    private Color originalColor;

    void Start()
    {
        bodyRenderer = GetComponent<Renderer>();

        if (bodyRenderer != null)
        {
            originalColor = bodyRenderer.material.color;
        }
    }

    public void Flash()
    {
        if (bodyRenderer == null)
            return;

        StopAllCoroutines();
        StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        bodyRenderer.material.color = flashColor;

        yield return new WaitForSeconds(flashDuration);

        bodyRenderer.material.color = originalColor;
    }
}