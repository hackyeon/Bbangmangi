using UnityEngine;

public class EnemyAttackAI : MonoBehaviour
{
    public Transform target;
    public float attackDistance = 2f;
    public float attackCooldown = 1.5f;

    private BatAttack batAttack;
    private float lastAttackTime;

    void Start()
    {
        batAttack = GetComponent<BatAttack>();
    }

    void Update()
    {
        if (target == null)
            return;

        float distance = Vector3.Distance(
            transform.position,
            target.position
        );

        if (distance <= attackDistance &&
            Time.time >= lastAttackTime + attackCooldown)
        {
            batAttack.Attack();
            lastAttackTime = Time.time;
        }
    }
}