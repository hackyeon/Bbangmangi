using Fusion;
using UnityEngine;

public class KnockbackReceiver : NetworkBehaviour
{
    public float stunDuration = 0.55f;
    public float damping = 3f;

    public bool IsStunned { get; private set; }
    public PlayerRef LastAttacker { get; private set; }

    private Vector3 knockbackVelocity;
    private float stunTimer;
    private HitFlash hitFlash;

    public override void Spawned()
    {
        hitFlash = GetComponent<HitFlash>();
    }

    public void RequestKnockback(Vector3 velocity, PlayerRef attacker)
    {
        RPC_ApplyKnockback(velocity, attacker);
    }

    [Rpc(RpcSources.All, RpcTargets.InputAuthority)]
    private void RPC_ApplyKnockback(Vector3 velocity, PlayerRef attacker)
    {
        LastAttacker = attacker;
        knockbackVelocity = velocity;
        stunTimer = stunDuration;
        IsStunned = true;

        if (HasInputAuthority)
            CameraShake.Instance?.Shake();

        hitFlash?.Flash();
    }

    public Vector3 ConsumeVelocity(float deltaTime)
    {
        if (stunTimer > 0f)
            stunTimer -= deltaTime;
        else
            IsStunned = false;

        Vector3 result = knockbackVelocity;

        knockbackVelocity = Vector3.Lerp(
            knockbackVelocity,
            Vector3.zero,
            damping * deltaTime
        );

        return result;
    }

    public void ClearLastAttacker()
    {
        LastAttacker = PlayerRef.None;
    }
}