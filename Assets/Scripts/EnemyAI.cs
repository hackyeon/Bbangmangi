using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    public Transform target;
    public float moveSpeed = 2f;
    public float stopDistance = 1.5f;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        if (target == null)
            return;

        Vector3 dir = target.position - transform.position;
        dir.y = 0;

        if (dir.magnitude <= stopDistance)
            return;

        Vector3 move = dir.normalized * moveSpeed;

        rb.velocity = new Vector3(
            move.x,
            rb.velocity.y,
            move.z
        );

        transform.forward = dir.normalized;
    }
}