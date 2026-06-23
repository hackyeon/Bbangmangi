using Fusion;
using UnityEngine;

public class NetworkPlayerAnimation : NetworkBehaviour
{
    public Animator animator;
    public NetworkPlayerMotor motor;
    public KnockbackReceiver knockbackReceiver;
    public CapsuleBatAttack capsuleBatAttack;
    
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsFallingHash = Animator.StringToHash("IsFalling");
    private static readonly int IsStunnedHash = Animator.StringToHash("IsStunned");
    private static readonly int AttackHash = Animator.StringToHash("Attack");

    [Networked] private float AnimSpeed { get; set; }
    [Networked] private NetworkBool AnimIsFalling { get; set; }
    [Networked] private NetworkBool AnimIsStunned { get; set; }
    [Networked] private int AttackVersion { get; set; }

    private int renderedAttackVersion;

    public override void Spawned()
    {
        SetupReferences();

        renderedAttackVersion = AttackVersion;
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority)
            return;

        AnimIsFalling = motor != null && !motor.IsGrounded;
        AnimIsStunned = knockbackReceiver != null && knockbackReceiver.IsStunned;

        AnimSpeed = 0f;

        if (!AnimIsStunned && GetInput(out BbangmangiInputData input))
            AnimSpeed = Mathf.Clamp01(input.MoveDirection.magnitude);
    }

    public override void Render()
    {
        SetupReferences();

        if (animator != null && animator.runtimeAnimatorController != null)
        {
            animator.SetFloat(SpeedHash, AnimSpeed);
            animator.SetBool(IsFallingHash, AnimIsFalling);
            animator.SetBool(IsStunnedHash, AnimIsStunned);
        }

        if (renderedAttackVersion != AttackVersion)
        {
            renderedAttackVersion = AttackVersion;

            if (animator != null && animator.runtimeAnimatorController != null)
                animator.SetTrigger(AttackHash);
            else if (capsuleBatAttack != null)
                capsuleBatAttack.PlaySwing();
        }
    }

    public void PlayAttack()
    {
        if (!HasStateAuthority)
            return;

        AttackVersion++;
    }

    private void SetupReferences()
    {
        if (capsuleBatAttack == null)
            capsuleBatAttack = GetComponentInChildren<CapsuleBatAttack>(true);
        
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (motor == null)
            motor = GetComponent<NetworkPlayerMotor>();

        if (knockbackReceiver == null)
            knockbackReceiver = GetComponent<KnockbackReceiver>();
    }
}
