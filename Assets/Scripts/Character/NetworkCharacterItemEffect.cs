using System.Collections.Generic;
using Fusion;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(
    typeof(NetworkPlayerStats),
    typeof(KnockbackReceiver),
    typeof(BatAttack)
)]
public class NetworkCharacterItemEffect : NetworkBehaviour
{
    [Header("Giant Hammer")]
    [SerializeField, Min(0.1f)] private float giantHammerDuration = 10f;
    [SerializeField, Min(1f)] private float giantHammerScaleMultiplier = 1.8f;
    [SerializeField, Min(1f)] private float giantHammerRangeMultiplier = 1.5f;

    [Header("Shield")]
    [SerializeField] private GameObject shieldVisual;

    [Header("Bomb")]
    [SerializeField, Min(0.1f)] private float bombRadius = 6f;
    [SerializeField, Min(0f)] private float bombKnockbackMultiplier = 1.5f;
    [SerializeField] private LayerMask bombTargetLayers = ~0;
    [SerializeField] private GameObject bombEffectPrefab;

    [Networked]
    public NetworkBool IsGiantHammerActive { get; private set; }

    [Networked]
    public TickTimer GiantHammerTimer { get; private set; }

    [Networked]
    public NetworkBool IsShieldActive { get; private set; }

    private readonly HashSet<KnockbackReceiver> bombTargets = new();
    private readonly Collider[] bombHitBuffer = new Collider[128];

    private NetworkPlayerStats playerStats;
    private BatAttack batAttack;
    private KnockbackReceiver knockbackReceiver;
    private bool hasPresentedState;
    private bool presentedGiantHammer;
    private bool presentedShield;

    public override void Spawned()
    {
        CacheComponents();
        RefreshPresentation(true);
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority)
            return;

        if (!NetworkRoundManager.IsGameplayActive)
        {
            if (IsGiantHammerActive || IsShieldActive)
                ResetTemporaryEffects();

            return;
        }

        if (IsGiantHammerActive &&
            GiantHammerTimer.ExpiredOrNotRunning(Runner))
        {
            IsGiantHammerActive = false;
            GiantHammerTimer = TickTimer.None;
            RefreshPresentation(true);
        }
    }

    public override void Render()
    {
        RefreshPresentation(false);
    }

    public bool TryApplyItem(BattleItemType itemType)
    {
        if (!HasStateAuthority || !NetworkRoundManager.IsGameplayActive)
            return false;

        switch (itemType)
        {
            case BattleItemType.GiantHammer:
                ActivateGiantHammer();
                return true;

            case BattleItemType.Shield:
                ActivateShield();
                return true;

            case BattleItemType.Bomb:
                TriggerBomb();
                return true;

            default:
                return false;
        }
    }

    public bool TryBlockKnockback()
    {
        if (!HasStateAuthority || !IsShieldActive)
            return false;

        IsShieldActive = false;
        RefreshPresentation(true);

        Debug.Log($"[Item] {GetDisplayName()} shield blocked knockback.");
        return true;
    }

    public void ResetTemporaryEffects()
    {
        if (!HasStateAuthority)
            return;

        IsGiantHammerActive = false;
        GiantHammerTimer = TickTimer.None;
        IsShieldActive = false;
        RefreshPresentation(true);
    }

    private void ActivateGiantHammer()
    {
        IsGiantHammerActive = true;
        GiantHammerTimer = TickTimer.CreateFromSeconds(
            Runner,
            giantHammerDuration
        );

        RefreshPresentation(true);
    }

    private void ActivateShield()
    {
        IsShieldActive = true;
        RefreshPresentation(true);
    }

    private void TriggerBomb()
    {
        CacheComponents();
        bombTargets.Clear();

        int hitCount = Physics.OverlapSphereNonAlloc(
            transform.position,
            bombRadius,
            bombHitBuffer,
            bombTargetLayers,
            QueryTriggerInteraction.Ignore
        );

        for (int hitIndex = 0; hitIndex < hitCount; hitIndex++)
        {
            Collider hit = bombHitBuffer[hitIndex];

            if (hit == null)
                continue;

            KnockbackReceiver receiver =
                hit.GetComponentInParent<KnockbackReceiver>();

            if (!IsValidBombTarget(receiver) || !bombTargets.Add(receiver))
                continue;

            Vector3 direction = receiver.transform.position - transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude < 0.001f)
                direction = transform.forward;

            direction.y = 0f;

            if (direction.sqrMagnitude < 0.001f)
                direction = Vector3.forward;

            direction.Normalize();

            float horizontalPower = batAttack != null
                ? batAttack.knockbackPower * bombKnockbackMultiplier
                : 0f;

            float upwardPower = batAttack != null
                ? batAttack.upwardPower
                : 0f;

            Vector3 velocity =
                direction * horizontalPower +
                Vector3.up * upwardPower;

            receiver.ApplyKnockback(
                velocity,
                Object.InputAuthority,
                Object
            );
        }

        if (bombEffectPrefab != null)
            RPC_PlayBombEffect(transform.position);

        Debug.Log(
            $"[Item] {GetDisplayName()} triggered Bomb " +
            $"for {bombTargets.Count} target(s)."
        );
    }

    private bool IsValidBombTarget(KnockbackReceiver receiver)
    {
        return receiver != null &&
               receiver != knockbackReceiver &&
               receiver.Object != null &&
               receiver.Object.IsValid &&
               receiver.Runner == Runner &&
               receiver.GetComponent<NetworkPlayerStats>() != null;
    }

    private void RefreshPresentation(bool force)
    {
        bool isGiantHammerActive = IsGiantHammerActive;
        bool isShieldActive = IsShieldActive;

        if (force ||
            !hasPresentedState ||
            presentedGiantHammer != isGiantHammerActive)
        {
            CacheComponents();

            float weaponScaleMultiplier = isGiantHammerActive
                ? giantHammerScaleMultiplier
                : 1f;

            float attackRangeMultiplier = isGiantHammerActive
                ? giantHammerRangeMultiplier
                : 1f;

            playerStats?.SetTemporaryItemModifiers(
                weaponScaleMultiplier,
                attackRangeMultiplier
            );

            presentedGiantHammer = isGiantHammerActive;
        }

        if (force ||
            !hasPresentedState ||
            presentedShield != isShieldActive)
        {
            if (shieldVisual != null)
                shieldVisual.SetActive(isShieldActive);

            presentedShield = isShieldActive;
        }

        hasPresentedState = true;
    }

    private void CacheComponents()
    {
        if (playerStats == null)
            playerStats = GetComponent<NetworkPlayerStats>();

        if (batAttack == null)
            batAttack = GetComponent<BatAttack>();

        if (knockbackReceiver == null)
            knockbackReceiver = GetComponent<KnockbackReceiver>();
    }

    private string GetDisplayName()
    {
        NetworkPlayerName playerName = GetComponent<NetworkPlayerName>();
        string displayName = playerName != null
            ? playerName.Nickname.ToString()
            : "";

        if (!string.IsNullOrEmpty(displayName))
            return displayName;

        return playerStats != null && playerStats.IsBot
            ? "Bot"
            : $"Player {Object.InputAuthority.PlayerId}";
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayBombEffect(Vector3 position)
    {
        if (bombEffectPrefab != null)
            Instantiate(bombEffectPrefab, position, Quaternion.identity);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.45f, 0.1f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, bombRadius);
    }
}
