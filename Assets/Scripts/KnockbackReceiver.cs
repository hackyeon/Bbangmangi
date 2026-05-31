using System.Collections;
using UnityEngine;

public class KnockbackReceiver : MonoBehaviour
{
    public float stunDuration = 0.4f;
    public bool isPlayer;
    public bool IsStunned { get; private set; }

    private Rigidbody rb;
    private HitFlash hitFlash;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        hitFlash = GetComponent<HitFlash>();
    }

    public void Knockback(Vector3 force)
    {
        if (isPlayer)
        {
            CameraShake.Instance?.Shake();
        }
        
        hitFlash?.Flash();
        
        StartCoroutine(KnockbackRoutine(force));
    }

    private IEnumerator KnockbackRoutine(Vector3 force)
    {
        IsStunned = true;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.AddForce(force, ForceMode.Impulse);

        yield return new WaitForSeconds(stunDuration);

        IsStunned = false;
    }
}