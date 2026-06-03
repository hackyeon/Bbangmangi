using Fusion;
using UnityEngine;

public class BatAttack : NetworkBehaviour
{
    public float attackRange = 2.2f;
    public float attackOffset = 1.2f;
    public float knockbackPower = 26f;
    public float upwardPower = 13f;

    public GameObject hitParticlePrefab;

    private bool isAttacking;

    public void Attack()
    {
        if (!HasStateAuthority)
            return;

        if (isAttacking)
            return;

        RPC_PlayAttack();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_PlayAttack()
    {
        if (isAttacking)
            return;

        isAttacking = true;

        NetworkPlayerAnimation playerAnimation =
            GetComponent<NetworkPlayerAnimation>();

        if (playerAnimation != null)
            playerAnimation.PlayAttack();

        Invoke(nameof(EndAttack), 0.45f);

        if (HasStateAuthority)
            Invoke(nameof(Hit), 0.18f);
    }

    private void EndAttack()
    {
        isAttacking = false;
    }

    private void Hit()
    {
        Vector3 attackPoint =
            transform.position + transform.forward * attackOffset;

        Collider[] hits = Physics.OverlapSphere(
            attackPoint,
            attackRange
        );

        foreach (Collider hit in hits)
        {
            KnockbackReceiver receiver =
                hit.GetComponentInParent<KnockbackReceiver>();

            if (receiver == null || receiver.gameObject == gameObject)
                continue;

            Vector3 dir = receiver.transform.position - transform.position;
            dir.y = 0f;

            if (dir.sqrMagnitude < 0.001f)
                dir = transform.forward;

            dir.Normalize();

            Vector3 velocity =
                dir * knockbackPower +
                Vector3.up * upwardPower;

            receiver.ApplyKnockback(velocity, Object.InputAuthority);

            if (hitParticlePrefab != null)
            {
                Instantiate(
                    hitParticlePrefab,
                    receiver.transform.position + Vector3.up,
                    Quaternion.identity
                );
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;

        Vector3 attackPoint =
            transform.position + transform.forward * attackOffset;

        Gizmos.DrawWireSphere(attackPoint, attackRange);
    }
}