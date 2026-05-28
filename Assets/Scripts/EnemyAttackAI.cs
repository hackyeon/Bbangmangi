using UnityEngine;

public class EnemyAttackAI : MonoBehaviour
{
    public Transform target;
    public float attackDistance = 3f;
    public float attackCooldown = 1f;

    private BatAttack batAttack;
    private KnockbackReceiver knockbackReceiver;
    private float lastAttackTime;

    void Start()
    {
        batAttack = GetComponent<BatAttack>();
        knockbackReceiver = GetComponent<KnockbackReceiver>();
    }

    void Update()
    {
        if (target == null || batAttack == null)
            return;

        if (knockbackReceiver != null && knockbackReceiver.IsStunned)
            return;

        Vector3 dir = target.position - transform.position;
        dir.y = 0;

        float distance = dir.magnitude;

        if (distance <= attackDistance &&
            Time.time >= lastAttackTime + attackCooldown)
        {
            transform.forward = dir.normalized;
            batAttack.Attack();
            lastAttackTime = Time.time;
        }
    }
}