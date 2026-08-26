using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class NetworkKillScaleVisual : MonoBehaviour
{
    private const float BaseScaleMultiplier = 1f;
    private const float ScalePerKill = 0.2f;
    private const float MaxScaleMultiplier = 2.6f;

    private static readonly string[] AutoTargetKeywords =
    {
        "WeaponSocket",
        "Weapon",
        "Sword",
        "Shield",
        "Bat",
        "Spatula",
        "Hammer"
    };

    private readonly List<ScaleTarget> scaleTargets = new();

    private NetworkPlayerScore playerScore;
    private CharacterData configuredCharacter;
    private Transform configuredVisualRoot;
    private int appliedKillCount = int.MinValue;
    private float temporaryScaleMultiplier = 1f;
    private float appliedTemporaryScaleMultiplier = float.NaN;

    public void Configure(CharacterData character, GameObject visualObject)
    {
        Transform visualRoot = visualObject != null ? visualObject.transform : null;

        if (configuredCharacter == character && configuredVisualRoot == visualRoot)
            return;

        RestoreOriginalScales();

        configuredCharacter = character;
        configuredVisualRoot = visualRoot;
        appliedKillCount = int.MinValue;
        appliedTemporaryScaleMultiplier = float.NaN;
        scaleTargets.Clear();

        if (configuredVisualRoot == null)
            return;

        CollectScaleTargets();
        ApplyCurrentScale();
    }

    public void ApplyCurrentScale()
    {
        if (configuredVisualRoot == null || scaleTargets.Count == 0)
            return;

        if (playerScore == null)
            playerScore = GetComponent<NetworkPlayerScore>();

        int killCount = playerScore != null ? playerScore.KillCount : 0;

        if (killCount == appliedKillCount &&
            Mathf.Approximately(
                temporaryScaleMultiplier,
                appliedTemporaryScaleMultiplier
            ))
        {
            return;
        }

        appliedKillCount = killCount;
        appliedTemporaryScaleMultiplier = temporaryScaleMultiplier;

        float scaleMultiplier =
            GetScaleMultiplier(killCount) * temporaryScaleMultiplier;

        foreach (ScaleTarget target in scaleTargets)
        {
            if (target.Transform == null)
                continue;

            target.Transform.localScale = target.OriginalScale * scaleMultiplier;
        }
    }

    public void SetTemporaryScaleMultiplier(float multiplier)
    {
        temporaryScaleMultiplier = Mathf.Max(0.01f, multiplier);
        ApplyCurrentScale();
    }

    private void CollectScaleTargets()
    {
        AddCapsuleBatTarget();

        if (scaleTargets.Count > 0)
            return;

        AddMarkedTargets();

        if (scaleTargets.Count > 0)
            return;

        AddNamedTargets();

        if (scaleTargets.Count > 0)
            return;

        AddAutoTargets();

        if (scaleTargets.Count == 0)
            AddScaleTarget(configuredVisualRoot);
    }

    private void AddCapsuleBatTarget()
    {
        CapsuleBatAttack capsuleBatAttack =
            configuredVisualRoot.GetComponent<CapsuleBatAttack>();

        if (capsuleBatAttack == null || capsuleBatAttack.batTransform == null)
            return;

        AddScaleTarget(capsuleBatAttack.batTransform);
    }

    private void AddMarkedTargets()
    {
        KillScaleTarget[] markedTargets =
            configuredVisualRoot.GetComponentsInChildren<KillScaleTarget>(true);

        foreach (KillScaleTarget markedTarget in markedTargets)
        {
            if (markedTarget != null)
                AddScaleTarget(markedTarget.transform);
        }
    }

    private void AddNamedTargets()
    {
        if (configuredCharacter == null ||
            configuredCharacter.killScaleTargetNames == null)
        {
            return;
        }

        foreach (string targetName in configuredCharacter.killScaleTargetNames)
        {
            if (string.IsNullOrWhiteSpace(targetName))
                continue;

            Transform target = FindChildByName(
                configuredVisualRoot,
                targetName.Trim()
            );

            if (target != null)
                AddScaleTarget(target);
        }
    }

    private void AddAutoTargets()
    {
        foreach (Transform child in configuredVisualRoot.GetComponentsInChildren<Transform>(true))
        {
            if (child == configuredVisualRoot)
                continue;

            if (!IsAutoScaleTarget(child.name))
                continue;

            if (HasAncestorScaleTarget(child))
                continue;

            AddScaleTarget(child);
        }
    }

    private static Transform FindChildByName(Transform root, string targetName)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (string.Equals(
                    child.name,
                    targetName,
                    StringComparison.OrdinalIgnoreCase))
            {
                return child;
            }
        }

        return null;
    }

    private static bool IsAutoScaleTarget(string objectName)
    {
        foreach (string keyword in AutoTargetKeywords)
        {
            if (objectName.IndexOf(
                    keyword,
                    StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }

    private bool HasAncestorScaleTarget(Transform target)
    {
        Transform current = target.parent;

        while (current != null && current != configuredVisualRoot.parent)
        {
            foreach (ScaleTarget scaleTarget in scaleTargets)
            {
                if (scaleTarget.Transform == current)
                    return true;
            }

            current = current.parent;
        }

        return false;
    }

    private void AddScaleTarget(Transform target)
    {
        if (target == null || ContainsScaleTarget(target))
            return;

        scaleTargets.Add(new ScaleTarget(target, target.localScale));
    }

    private bool ContainsScaleTarget(Transform target)
    {
        foreach (ScaleTarget scaleTarget in scaleTargets)
        {
            if (scaleTarget.Transform == target)
                return true;
        }

        return false;
    }

    private void RestoreOriginalScales()
    {
        foreach (ScaleTarget target in scaleTargets)
        {
            if (target.Transform != null)
                target.Transform.localScale = target.OriginalScale;
        }
    }

    private static float GetScaleMultiplier(int killCount)
    {
        int safeKillCount = Mathf.Max(0, killCount);
        float scaleMultiplier =
            BaseScaleMultiplier + safeKillCount * ScalePerKill;

        return Mathf.Min(scaleMultiplier, MaxScaleMultiplier);
    }

    private readonly struct ScaleTarget
    {
        public readonly Transform Transform;
        public readonly Vector3 OriginalScale;

        public ScaleTarget(Transform transform, Vector3 originalScale)
        {
            Transform = transform;
            OriginalScale = originalScale;
        }
    }
}
