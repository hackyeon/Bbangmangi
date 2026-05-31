using System.Collections;
using UnityEngine;

public class BatAttack : MonoBehaviour
{
    public Transform bat;
    public GameObject hitParticlePrefab;

    public float attackRange = 2.2f;
    public float attackOffset = 1.2f;
    public float knockbackPower = 35f;
    public float upwardPower = 3f;

    public float attackDuration = 0.12f;

    private bool isAttacking;

    public void Attack()
    {
        if (isAttacking || bat == null)
            return;

        StartCoroutine(AttackRoutine());
    }

    IEnumerator AttackRoutine()
    {
        isAttacking = true;

        Quaternion startRot = bat.localRotation;
        Quaternion endRot = startRot * Quaternion.Euler(0, 0, -120);

        float time = 0;
        bool didHit = false;

        while (time < attackDuration)
        {
            float t = time / attackDuration;

            bat.localRotation = Quaternion.Slerp(startRot, endRot, t);

            if (!didHit && t >= 0.45f)
            {
                Hit();
                didHit = true;
            }

            time += Time.deltaTime;
            yield return null;
        }

        time = 0;

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

    void Hit()
    {
        Vector3 attackPoint =
            transform.position + transform.forward * attackOffset;

        Collider[] hits = Physics.OverlapSphere(
            attackPoint,
            attackRange
        );

        foreach (Collider hit in hits)
        {
            if (hit.gameObject == gameObject)
                continue;

            Rigidbody rb = hit.GetComponent<Rigidbody>();

            if (rb == null)
                continue;

            Vector3 dir = hit.transform.position - transform.position;
            dir.y = 0;
            dir.Normalize();

            Vector3 force =
                dir * knockbackPower + Vector3.up * upwardPower;

            KnockbackReceiver receiver =
                hit.GetComponent<KnockbackReceiver>();

            if (receiver != null)
            {
                receiver.Knockback(force);
                
                if (hitParticlePrefab != null)
                {
                    Instantiate(
                        hitParticlePrefab,
                        hit.transform.position + Vector3.up,
                        Quaternion.identity
                    );
                }
            }
            else
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.AddForce(force, ForceMode.Impulse);
            }

            Debug.Log($"Hit: {hit.gameObject.name}");
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;

        Vector3 attackPoint =
            transform.position + transform.forward * attackOffset;

        Gizmos.DrawWireSphere(attackPoint, attackRange);
    }
}