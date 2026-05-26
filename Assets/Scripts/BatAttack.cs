using UnityEngine;

public class BatAttack : MonoBehaviour
{
    public float attackRange = 1.5f;
    public float attackOffset = 1.5f;
    public float knockbackPower = 20f;

    public void Attack()
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

            if (rb != null)
            {
                Vector3 dir =
                    (hit.transform.position - transform.position).normalized;

                rb.AddForce(dir * knockbackPower, ForceMode.Impulse);
            }
        }
    }

    // void OnDrawGizmos()
    // {
    //     Gizmos.color = Color.red;
    //
    //     Vector3 attackPoint =
    //         transform.position + transform.forward * attackOffset;
    //
    //     Gizmos.DrawWireSphere(attackPoint, attackRange);
    // }
}