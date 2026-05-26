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
            batAttack.Attack();
        }
    }
}