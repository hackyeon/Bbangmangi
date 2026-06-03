using Fusion;
using UnityEngine;

public class NetworkPlayerAnimation : NetworkBehaviour
{
    public Animator animator;
    public NetworkPlayerMotor motor;
    public KnockbackReceiver knockbackReceiver;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsFallingHash = Animator.StringToHash("IsFalling");
    private static readonly int IsStunnedHash = Animator.StringToHash("IsStunned");
    private static readonly int AttackHash = Animator.StringToHash("Attack");

    private Vector3 lastPosition;

    public override void Spawned()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (motor == null)
            motor = GetComponent<NetworkPlayerMotor>();

        if (knockbackReceiver == null)
            knockbackReceiver = GetComponent<KnockbackReceiver>();

        lastPosition = transform.position;
    }

    private void LateUpdate()
    {
        if (animator == null || animator.runtimeAnimatorController == null)
            return;

        Vector3 currentPosition = transform.position;
        Vector3 delta = currentPosition - lastPosition;
        delta.y = 0f;

        float speed = delta.magnitude / Mathf.Max(Time.deltaTime, 0.0001f);

        animator.SetFloat(SpeedHash, speed);
        animator.SetBool(IsStunnedHash, knockbackReceiver != null && knockbackReceiver.IsStunned);
        animator.SetBool(IsFallingHash, IsFalling());

        lastPosition = currentPosition;
    }

    public void PlayAttack()
    {
        if (animator == null || animator.runtimeAnimatorController == null)
            return;

        animator.SetTrigger(AttackHash);
    }

    private bool IsFalling()
    {
        if (motor == null)
            return false;

        return !motor.IsGrounded;
    }
}