using Fusion;
using UnityEngine;

public class KnockbackReceiver : NetworkBehaviour
{
    public float stunDuration = 0.55f;
    public float damping = 3f;
    public float stopSpeed = 0.05f;

    public bool IsStunned => isStunned;
    public NetworkObject LastAttackerObject => ResolveLastAttackerObject();
    public float LastHitAge => Time.time - lastHitTime;

    [Networked] public PlayerRef LastAttacker { get; private set; }
    [Networked] private NetworkId LastAttackerObjectId { get; set; }
    [Networked] private NetworkBool isStunned { get; set; }
    [Networked] private Vector3 KnockbackVelocity { get; set; }
    [Networked] private TickTimer StunTimer { get; set; }

    private HitFlash hitFlash;
    private NetworkObject lastAttackerObject;
    private float lastHitTime = -999f;

    public override void Spawned()
    {
        hitFlash = GetComponent<HitFlash>();
    }

    public bool ApplyKnockback(
        Vector3 velocity,
        PlayerRef attacker,
        NetworkObject attackerObject = null
    )
    {
        if (!HasStateAuthority)
            return false;

        if (velocity.sqrMagnitude > 0.001f &&
            IsHostileKnockback(attacker, attackerObject))
        {
            NetworkCharacterItemEffect itemEffect =
                GetComponent<NetworkCharacterItemEffect>();

            if (itemEffect != null && itemEffect.TryBlockKnockback())
                return false;
        }

        LastAttacker = attacker;
        LastAttackerObjectId =
            attackerObject != null && attackerObject.IsValid
                ? attackerObject.Id
                : default;

        lastAttackerObject = attackerObject;
        lastHitTime = Time.time;
        KnockbackVelocity = velocity;
        StunTimer = TickTimer.CreateFromSeconds(Runner, stunDuration);
        isStunned = true;

        RPC_PlayHitFlash();
        return true;
    }

    public Vector3 ConsumeVelocity(float deltaTime)
    {
        if (StunTimer.ExpiredOrNotRunning(Runner))
            isStunned = false;

        Vector3 result = KnockbackVelocity;

        KnockbackVelocity = Vector3.Lerp(
            KnockbackVelocity,
            Vector3.zero,
            damping * deltaTime
        );

        if (KnockbackVelocity.sqrMagnitude <= stopSpeed * stopSpeed)
            KnockbackVelocity = Vector3.zero;

        return result;
    }

    public void ClearLastAttacker()
    {
        if (!HasStateAuthority)
            return;

        LastAttacker = PlayerRef.None;
        LastAttackerObjectId = default;
        lastAttackerObject = null;
    }

    private NetworkObject ResolveLastAttackerObject()
    {
        if (lastAttackerObject != null &&
            lastAttackerObject.IsValid &&
            lastAttackerObject.Id == LastAttackerObjectId)
        {
            return lastAttackerObject;
        }

        lastAttackerObject = null;

        if (!LastAttackerObjectId.IsValid || Runner == null)
            return null;

        if (Runner.TryFindObject(
                LastAttackerObjectId,
                out NetworkObject resolvedObject) &&
            resolvedObject != null &&
            resolvedObject.IsValid)
        {
            lastAttackerObject = resolvedObject;
        }

        return lastAttackerObject;
    }

    private bool IsHostileKnockback(
        PlayerRef attacker,
        NetworkObject attackerObject
    )
    {
        if (attackerObject != null)
            return attackerObject != Object;

        return attacker != PlayerRef.None &&
               attacker != Object.InputAuthority;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_PlayHitFlash()
    {
        if (hitFlash == null)
            hitFlash = GetComponent<HitFlash>();

        hitFlash?.Flash();
    }
}
