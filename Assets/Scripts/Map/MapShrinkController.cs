using UnityEngine;

[DisallowMultipleComponent]
public sealed class MapShrinkController : MonoBehaviour
{
    public static MapShrinkController Instance { get; private set; }

    private static bool hasWarnedMissingController;

    [Header("Play Area")]
    [SerializeField] private Transform playAreaTransform;
    [SerializeField] private Collider playAreaCollider;

    [Header("Round Timer Thresholds")]
    [SerializeField, Min(0.1f)]
    private float shrinkStartRemainingTime = 60f;

    [SerializeField, Min(0f)]
    private float finalShrinkStartRemainingTime = 30f;

    [Header("Horizontal Scale Multipliers")]
    [SerializeField, Range(0.05f, 1f)]
    private float phase1TargetScale = 0.8f;

    [SerializeField, Range(0.05f, 1f)]
    private float finalTargetScale = 0.55f;

    public float CurrentScaleMultiplier { get; private set; } = 1f;

    public Vector3 PlayAreaCenter
    {
        get
        {
            if (!TryInitialize())
                return transform.position;

            return playAreaCollider.bounds.center;
        }
    }

    private Vector3 initialLocalScale;
    private Vector3 initialLocalPosition;
    private NetworkRoundManager roundManager;
    private bool isInitialized;
    private bool hasLoggedInvalidSetup;

    private void Reset()
    {
        playAreaTransform = transform;
        playAreaCollider = GetComponent<Collider>();
    }

    private void Awake()
    {
        RegisterInstance();
        TryInitialize();
    }

    private void OnEnable()
    {
        RegisterInstance();
    }

    private void Update()
    {
        NetworkRoundManager activeRoundManager =
            NetworkRoundManager.Instance;

        if (activeRoundManager == null)
            return;

        ApplyRoundState(activeRoundManager);
    }

    private void OnDisable()
    {
        if (Instance == this)
            Instance = null;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void OnValidate()
    {
        shrinkStartRemainingTime = Mathf.Max(
            0.1f,
            shrinkStartRemainingTime
        );

        finalShrinkStartRemainingTime = Mathf.Clamp(
            finalShrinkStartRemainingTime,
            0f,
            shrinkStartRemainingTime
        );

        phase1TargetScale = Mathf.Clamp(
            phase1TargetScale,
            0.05f,
            1f
        );

        finalTargetScale = Mathf.Clamp(
            finalTargetScale,
            0.05f,
            phase1TargetScale
        );
    }

    public static void RefreshActive(NetworkRoundManager activeRoundManager)
    {
        MapShrinkController controller = Instance;

        if (controller == null)
        {
            controller = FindFirstObjectByType<MapShrinkController>();
        }

        if (controller == null)
        {
            if (!hasWarnedMissingController &&
                activeRoundManager != null &&
                activeRoundManager.HasStateAuthority)
            {
                hasWarnedMissingController = true;
                Debug.LogWarning(
                    "[Map] MapShrinkController was not found in the scene.",
                    activeRoundManager
                );
            }

            return;
        }

        controller?.ApplyRoundState(activeRoundManager);
    }

    public void ApplyRoundState(NetworkRoundManager activeRoundManager)
    {
        if (activeRoundManager == null || !TryInitialize())
            return;

        roundManager = activeRoundManager;

        float scaleMultiplier = GetScaleMultiplier(
            roundManager.Phase,
            roundManager.GetRemainingTime()
        );

        ApplyScale(scaleMultiplier);
    }

    public float GetSafeHorizontalRadius(float edgePadding)
    {
        if (!TryInitialize())
            return 0f;

        edgePadding = Mathf.Max(0f, edgePadding);

        if (playAreaCollider is BoxCollider boxCollider)
        {
            Vector3 lossyScale = boxCollider.transform.lossyScale;
            float halfWidth =
                Mathf.Abs(boxCollider.size.x * lossyScale.x) * 0.5f;

            float halfDepth =
                Mathf.Abs(boxCollider.size.z * lossyScale.z) * 0.5f;

            return Mathf.Max(
                0f,
                Mathf.Min(halfWidth, halfDepth) - edgePadding
            );
        }

        Bounds bounds = playAreaCollider.bounds;

        return Mathf.Max(
            0f,
            Mathf.Min(bounds.extents.x, bounds.extents.z) - edgePadding
        );
    }

    public bool TryGetRandomSpawnPosition(
        float requestedRadius,
        float worldY,
        float edgePadding,
        out Vector3 spawnPosition
    )
    {
        spawnPosition = default;

        if (!TryInitialize())
            return false;

        float safeRadius = Mathf.Min(
            Mathf.Max(0f, requestedRadius),
            GetSafeHorizontalRadius(edgePadding)
        );

        Vector2 randomOffset = safeRadius > 0.01f
            ? Random.insideUnitCircle * safeRadius
            : Vector2.zero;

        if (playAreaCollider is BoxCollider boxCollider)
        {
            Vector3 lossyScale = boxCollider.transform.lossyScale;
            float scaleX = Mathf.Max(0.0001f, Mathf.Abs(lossyScale.x));
            float scaleZ = Mathf.Max(0.0001f, Mathf.Abs(lossyScale.z));

            Vector3 localPoint = boxCollider.center + new Vector3(
                randomOffset.x / scaleX,
                0f,
                randomOffset.y / scaleZ
            );

            spawnPosition = boxCollider.transform.TransformPoint(localPoint);
        }
        else
        {
            Vector3 center = playAreaCollider.bounds.center;
            spawnPosition = center + new Vector3(
                randomOffset.x,
                0f,
                randomOffset.y
            );
        }

        spawnPosition.y = worldY;
        return true;
    }

    public float GetScaleMultiplier(
        RoundPhase phase,
        float remainingTime
    )
    {
        switch (phase)
        {
            case RoundPhase.Playing:
                return CalculatePlayingScale(remainingTime);

            case RoundPhase.Result:
                return finalTargetScale;

            default:
                return 1f;
        }
    }

    private float CalculatePlayingScale(float remainingTime)
    {
        remainingTime = Mathf.Max(0f, remainingTime);

        if (remainingTime >= shrinkStartRemainingTime)
            return 1f;

        if (remainingTime > finalShrinkStartRemainingTime)
        {
            float duration = Mathf.Max(
                0.0001f,
                shrinkStartRemainingTime - finalShrinkStartRemainingTime
            );

            float progress = Mathf.Clamp01(
                (shrinkStartRemainingTime - remainingTime) / duration
            );

            return Mathf.Lerp(1f, phase1TargetScale, progress);
        }

        float finalDuration = Mathf.Max(
            0.0001f,
            finalShrinkStartRemainingTime
        );

        float finalProgress = Mathf.Clamp01(
            (finalShrinkStartRemainingTime - remainingTime) / finalDuration
        );

        return Mathf.Clamp(
            Mathf.Lerp(
                phase1TargetScale,
                finalTargetScale,
                finalProgress
            ),
            finalTargetScale,
            1f
        );
    }

    private void ApplyScale(float scaleMultiplier)
    {
        scaleMultiplier = Mathf.Clamp(
            scaleMultiplier,
            finalTargetScale,
            1f
        );

        Vector3 targetScale = new Vector3(
            initialLocalScale.x * scaleMultiplier,
            initialLocalScale.y,
            initialLocalScale.z * scaleMultiplier
        );

        if ((playAreaTransform.localScale - targetScale).sqrMagnitude <
            0.0000001f)
        {
            CurrentScaleMultiplier = scaleMultiplier;
            return;
        }

        playAreaTransform.localPosition = initialLocalPosition;
        playAreaTransform.localScale = targetScale;
        CurrentScaleMultiplier = scaleMultiplier;

        Physics.SyncTransforms();
    }

    private bool TryInitialize()
    {
        if (isInitialized)
            return true;

        if (playAreaTransform == null)
            playAreaTransform = transform;

        if (playAreaCollider == null)
        {
            playAreaCollider =
                playAreaTransform.GetComponentInChildren<Collider>();
        }

        if (playAreaCollider == null ||
            (playAreaCollider.transform != playAreaTransform &&
             !playAreaCollider.transform.IsChildOf(playAreaTransform)))
        {
            LogInvalidSetupOnce(
                "Play Area Collider must be on the Play Area Transform " +
                "or one of its children."
            );

            return false;
        }

        initialLocalScale = playAreaTransform.localScale;
        initialLocalPosition = playAreaTransform.localPosition;

        if (Mathf.Abs(initialLocalScale.x) <= 0.0001f ||
            Mathf.Abs(initialLocalScale.z) <= 0.0001f)
        {
            LogInvalidSetupOnce(
                "Play Area X/Z scale must be greater than zero."
            );

            return false;
        }

        isInitialized = true;
        CurrentScaleMultiplier = 1f;
        return true;
    }

    private void RegisterInstance()
    {
        hasWarnedMissingController = false;

        if (Instance != null && Instance != this)
        {
            Debug.LogWarning(
                "[Map] Multiple MapShrinkController components found.",
                this
            );
        }

        Instance = this;
    }

    private void LogInvalidSetupOnce(string message)
    {
        if (hasLoggedInvalidSetup)
            return;

        hasLoggedInvalidSetup = true;
        Debug.LogError($"[Map] {message}", this);
    }
}
