using Fusion;
using UnityEngine;

public class KnockbackReceiver : NetworkBehaviour
{
    public Transform visualRoot;

    public float stunDuration = 0.55f;
    public float damping = 3f;
    public float previewDamping = 8f;

    public bool IsStunned { get; private set; }
    public PlayerRef LastAttacker { get; private set; }

    private Vector3 knockbackVelocity;
    private Vector3 previewVelocity;
    private Vector3 visualOffset;

    private float stunTimer;
    private HitFlash hitFlash;

    public override void Spawned()
    {
        hitFlash = GetComponent<HitFlash>();

        if (visualRoot == null)
            visualRoot = transform;
    }

    public void RequestKnockback(Vector3 velocity, PlayerRef attacker)
    {
        // 공격자 화면의 상대 캐릭터를 즉시 반응시킴
        if (!HasStateAuthority)
        {
            previewVelocity = velocity;
        }

        RPC_ApplyKnockback(velocity, attacker);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_ApplyKnockback(Vector3 velocity, PlayerRef attacker)
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

    private void LateUpdate()
    {
        if (visualRoot == null)
            return;

        if (previewVelocity.sqrMagnitude > 0.001f)
        {
            visualOffset += previewVelocity * Time.deltaTime;

            previewVelocity = Vector3.Lerp(
                previewVelocity,
                Vector3.zero,
                previewDamping * Time.deltaTime
            );
        }

        visualOffset = Vector3.Lerp(
            visualOffset,
            Vector3.zero,
            previewDamping * Time.deltaTime
        );

        visualRoot.localPosition = visualOffset;
    }

    public void ClearLastAttacker()
    {
        LastAttacker = PlayerRef.None;
    }
}