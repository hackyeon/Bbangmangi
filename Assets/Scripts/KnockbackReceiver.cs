using System.Collections;
using Fusion;
using UnityEngine;

public class KnockbackReceiver : NetworkBehaviour
{
    public float stunDuration = 0.4f;
    public bool isPlayer;

    public bool IsStunned { get; private set; }

    public PlayerRef LastAttacker { get; private set; }

    private Rigidbody rb;
    private HitFlash hitFlash;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        hitFlash = GetComponent<HitFlash>();
    }

    public void Knockback(Vector3 force, PlayerRef attacker)
    {
        LastAttacker = attacker;

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

    public void ClearLastAttacker()
    {
        LastAttacker = PlayerRef.None;
    }
}