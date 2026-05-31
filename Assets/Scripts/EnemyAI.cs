using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    public Transform target;
    public float moveSpeed = 2f;
    public float stopDistance = 1.5f;

    private Rigidbody rb;
    private KnockbackReceiver knockbackReceiver;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        knockbackReceiver = GetComponent<KnockbackReceiver>();
    }

    void FixedUpdate()
    {
        if (target == null)
            return;

        if (knockbackReceiver != null && knockbackReceiver.IsStunned)
            return;

        Vector3 dir = target.position - transform.position;
        dir.y = 0;

        if (dir.magnitude <= stopDistance)
        {
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
            return;
        }

        Vector3 move = dir.normalized * moveSpeed;

        rb.linearVelocity = new Vector3(
            move.x,
            rb.linearVelocity.y,
            move.z
        );

        transform.forward = dir.normalized;
    }
}