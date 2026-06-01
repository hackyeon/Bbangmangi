using System.Collections;
using Fusion;
using UnityEngine;

public class BatAttack : NetworkBehaviour
{
    public Transform bat;

    public float attackRange = 2.2f;
    public float attackOffset = 1.2f;
    public float knockbackPower = 12f;
    public float upwardPower = 4f;
    public float attackDuration = 0.09f;

    public GameObject hitParticlePrefab;

    private bool isAttacking;

    public void Attack()
    {
        if (isAttacking || bat == null)
            return;

        StartCoroutine(AttackRoutine());
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;

        Quaternion startRot = bat.localRotation;
        Quaternion endRot = startRot * Quaternion.Euler(0f, 0f, -120f);

        float time = 0f;
        bool didHit = false;

        while (time < attackDuration)
        {
            float t = time / attackDuration;

            bat.localRotation = Quaternion.Slerp(startRot, endRot, t);

            if (!didHit && t >= 0.15f)
            {
                Hit();
                didHit = true;
            }

            time += Time.deltaTime;
            yield return null;
        }

        time = 0f;

        while (time < attackDuration)
        {
            float t = time / attackDuration;

            bat.localRotation = Quaternion.Slerp(endRot, startRot, t);

            time += Time.deltaTime;
            yield return null;
        }

        bat.localRotation = startRot;
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

            receiver.RequestKnockback(velocity, Object.InputAuthority);

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