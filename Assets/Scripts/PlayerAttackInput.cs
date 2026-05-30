using UnityEngine;

public class PlayerAttackInput : MonoBehaviour
{
    private BatAttack batAttack;

    void Start()
    {
        batAttack = GetComponent<BatAttack>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            AttackByCurrentForward();
        }
    }

    public void Attack(Vector2 direction)
    {
        if (direction == Vector2.zero)
            return;

        Vector3 attackDir = new Vector3(direction.x, 0, direction.y);

        transform.forward = attackDir.normalized;

        batAttack.Attack();
    }

    private void AttackByCurrentForward()
    {
        batAttack.Attack();
    }
}