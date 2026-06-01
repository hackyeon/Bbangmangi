using Fusion;
using UnityEngine;

public class KnockbackReceiver : NetworkBehaviour
{
    public float stunDuration = 0.55f;
    public float damping = 3f;

    public bool IsStunned { get; private set; }
    public PlayerRef LastAttacker { get; private set; }

    private Vector3 knockbackVelocity;
    private Vector3 previewVelocity;
    private float stunTimer;
    private HitFlash hitFlash;

    public override void Spawned()
    {
        hitFlash = GetComponent<HitFlash>();
    }

    public void RequestKnockback(Vector3 velocity, PlayerRef attacker)
    {
        ApplyPreviewKnockback(velocity);
        RPC_ApplyKnockback(velocity, attacker);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_ApplyKnockback(Vector3 velocity, PlayerRef attacker)
    {
        ApplyKnockbackLocal(velocity, attacker, true);
    }

    private void ApplyKnockbackLocal(
        Vector3 velocity,
        PlayerRef attacker,
        bool shakeCamera
    )
    {
        LastAttacker = attacker;
        knockbackVelocity = velocity;
        stunTimer = stunDuration;
        IsStunned = true;

        if (shakeCamera && HasInputAuthority)
            CameraShake.Instance?.Shake();

        hitFlash?.Flash();
    }

    private void ApplyPreviewKnockback(Vector3 velocity)
    {
        if (HasInputAuthority)
            return;

        previewVelocity = velocity;
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

    private void LateUpdate()
    {
        if (previewVelocity.sqrMagnitude <= 0.001f)
            return;

        transform.position += previewVelocity * Time.deltaTime;

        previewVelocity = Vector3.Lerp(
            previewVelocity,
            Vector3.zero,
            damping * Time.deltaTime
        );
    }

    public void ClearLastAttacker()
    {
        LastAttacker = PlayerRef.None;
    }
}