using System.Collections;
using UnityEngine;

public class HitFlash : MonoBehaviour
{
    public Color flashColor = Color.white;
    public float flashDuration = 0.08f;

    private Renderer[] renderers;
    private Color[][] originalColors;
    private Coroutine flashCoroutine;

    public void Flash()
    {
        RefreshRenderers();

        if (renderers == null || renderers.Length == 0)
            return;

        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);

        flashCoroutine = StartCoroutine(FlashRoutine());
    }

    private void RefreshRenderers()
    {
        renderers = GetComponentsInChildren<Renderer>(true);
        originalColors = new Color[renderers.Length][];

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null)
                continue;

            Material[] materials = renderers[i].materials;
            originalColors[i] = new Color[materials.Length];

            for (int j = 0; j < materials.Length; j++)
            {
                originalColors[i][j] =
                    materials[j].HasProperty("_Color")
                        ? materials[j].color
                        : Color.white;
            }
        }
    }

    private IEnumerator FlashRoutine()
    {
        SetRendererColor(flashColor);

        yield return new WaitForSeconds(flashDuration);

        RestoreRendererColors();
        flashCoroutine = null;
    }

    private void SetRendererColor(Color color)
    {
        foreach (Renderer targetRenderer in renderers)
        {
            if (targetRenderer == null)
                continue;

            Material[] materials = targetRenderer.materials;

            foreach (Material material in materials)
            {
                if (material.HasProperty("_Color"))
                    material.color = color;
            }
        }
    }

    private void RestoreRendererColors()
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null || originalColors[i] == null)
                continue;

            Material[] materials = renderers[i].materials;

            for (int j = 0; j < materials.Length; j++)
            {
                if (j >= originalColors[i].Length)
                    continue;

                if (materials[j].HasProperty("_Color"))
                    materials[j].color = originalColors[i][j];
            }
        }
    }
}
