using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class BatAttack : NetworkBehaviour
{
    public float attackRange = 2.2f;
    public float attackOffset = 1.2f;
    [Range(1f, 180f)] public float attackAngle = 115f;
    public LayerMask hitLayers = ~0;
    public float knockbackPower = 26f;
    public float upwardPower = 13f;
    public float attackCooldown = 0.45f;
    public float hitDelay = 0.18f;

    public GameObject hitParticlePrefab;

    [Networked] private TickTimer AttackCooldownTimer { get; set; }
    [Networked] private TickTimer HitDelayTimer { get; set; }
    [Networked] private NetworkBool HasPendingHit { get; set; }
    [Networked] private Vector3 PendingAttackDirection { get; set; }

    private readonly HashSet<KnockbackReceiver> hitReceivers = new();

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority)
            return;

        if (!HasPendingHit || !HitDelayTimer.Expired(Runner))
            return;

        HasPendingHit = false;
        Hit(PendingAttackDirection);
    }

    public void Attack()
    {
        if (!HasStateAuthority)
            return;

        if (!AttackCooldownTimer.ExpiredOrNotRunning(Runner))
            return;

        Vector3 attackDirection = transform.forward;
        attackDirection.y = 0f;

        if (attackDirection.sqrMagnitude < 0.001f)
            attackDirection = Vector3.forward;

        PendingAttackDirection = attackDirection.normalized;
        AttackCooldownTimer = TickTimer.CreateFromSeconds(Runner, attackCooldown);
        HitDelayTimer = TickTimer.CreateFromSeconds(Runner, hitDelay);
        HasPendingHit = true;

        NetworkPlayerAnimation playerAnimation =
            GetComponent<NetworkPlayerAnimation>();

        if (playerAnimation != null)
            playerAnimation.PlayAttack();
    }

    private void Hit(Vector3 attackDirection)
    {
        if (!HasStateAuthority)
            return;

        hitReceivers.Clear();

        Vector3 attackPoint =
            transform.position + attackDirection * attackOffset;

        Collider[] hits = Physics.OverlapSphere(
            attackPoint,
            attackRange,
            hitLayers,
            QueryTriggerInteraction.Ignore
        );

        foreach (Collider hit in hits)
        {
            KnockbackReceiver receiver =
                hit.GetComponentInParent<KnockbackReceiver>();

            if (receiver == null || receiver.gameObject == gameObject)
                continue;

            if (!hitReceivers.Add(receiver))
                continue;

            if (!IsInsideAttackArc(receiver.transform.position, attackDirection))
                continue;

            Vector3 dir = receiver.transform.position - transform.position;
            dir.y = 0f;

            if (dir.sqrMagnitude < 0.001f)
                dir = attackDirection;

            dir.Normalize();

            Vector3 velocity =
                dir * knockbackPower +
                Vector3.up * upwardPower;

            receiver.ApplyKnockback(velocity, Object.InputAuthority);
            RPC_PlayHitEffects(receiver.transform.position + Vector3.up, receiver.Object.InputAuthority);
        }
    }

    private bool IsInsideAttackArc(Vector3 targetPosition, Vector3 attackDirection)
    {
        Vector3 toTarget = targetPosition - transform.position;
        toTarget.y = 0f;

        if (toTarget.sqrMagnitude < 0.001f)
            return true;

        float angle = Vector3.Angle(attackDirection, toTarget);
        return angle <= attackAngle * 0.5f;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_PlayHitEffects(Vector3 hitPosition, PlayerRef targetPlayer)
    {
        if (hitParticlePrefab != null)
        {
            Instantiate(
                hitParticlePrefab,
                hitPosition,
                Quaternion.identity
            );
        }

        if (Runner != null &&
            Runner.LocalPlayer == targetPlayer &&
            CameraShake.Instance != null)
        {
            CameraShake.Instance.Shake();
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;

        Vector3 attackDirection = transform.forward;
        attackDirection.y = 0f;

        if (attackDirection.sqrMagnitude < 0.001f)
            attackDirection = Vector3.forward;

        attackDirection.Normalize();

        Vector3 attackPoint =
            transform.position + attackDirection * attackOffset;

        Gizmos.DrawWireSphere(attackPoint, attackRange);

        Gizmos.color = Color.yellow;

        Quaternion leftRotation =
            Quaternion.AngleAxis(-attackAngle * 0.5f, Vector3.up);

        Quaternion rightRotation =
            Quaternion.AngleAxis(attackAngle * 0.5f, Vector3.up);

        Gizmos.DrawRay(transform.position, leftRotation * attackDirection * attackRange);
        Gizmos.DrawRay(transform.position, rightRotation * attackDirection * attackRange);
    }
}
