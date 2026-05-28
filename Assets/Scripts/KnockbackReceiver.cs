using System.Collections;
using UnityEngine;

public class KnockbackReceiver : MonoBehaviour
{
    public float stunDuration = 0.4f;

    public bool IsStunned { get; private set; }

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void Knockback(Vector3 force)
    {
        StartCoroutine(KnockbackRoutine(force));
    }

    private IEnumerator KnockbackRoutine(Vector3 force)
    {
        IsStunned = true;

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.AddForce(force, ForceMode.Impulse);

        yield return new WaitForSeconds(stunDuration);

        IsStunned = false;
    }
}