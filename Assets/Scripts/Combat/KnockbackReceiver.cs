using Fusion;
using UnityEngine;

public class KnockbackReceiver : NetworkBehaviour
{
    public float stunDuration = 0.55f;
    public float damping = 3f;
    public float stopSpeed = 0.05f;

    public bool IsStunned => isStunned;
    public NetworkObject LastAttackerObject { get; private set; }
    public float LastHitAge => Time.time - lastHitTime;

    [Networked] public PlayerRef LastAttacker { get; private set; }
    [Networked] private NetworkBool isStunned { get; set; }
    [Networked] private Vector3 KnockbackVelocity { get; set; }
    [Networked] private TickTimer StunTimer { get; set; }

    private HitFlash hitFlash;
    private float lastHitTime = -999f;

    public override void Spawned()
    {
        hitFlash = GetComponent<HitFlash>();
    }

    public void ApplyKnockback(
        Vector3 velocity,
        PlayerRef attacker,
        NetworkObject attackerObject = null
    )
    {
        if (!HasStateAuthority)
            return;

        LastAttacker = attacker;
        LastAttackerObject = attackerObject;
        lastHitTime = Time.time;
        KnockbackVelocity = velocity;
        StunTimer = TickTimer.CreateFromSeconds(Runner, stunDuration);
        isStunned = true;

        RPC_PlayHitFlash();
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
        LastAttackerObject = null;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_PlayHitFlash()
    {
        if (hitFlash == null)
            hitFlash = GetComponent<HitFlash>();

        hitFlash?.Flash();
    }
}
