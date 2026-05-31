using Fusion;
using UnityEngine;

public class NetworkPlayerMotor : NetworkBehaviour
{
    public float moveSpeed = 6f;

    private Rigidbody rb;
    private KnockbackReceiver knockbackReceiver;
    private BatAttack batAttack;
    
    public float gravity = -25f;
    public float verticalVelocity;
    public float groundCheckDistance = 1.1f;
    public LayerMask groundLayer;

    public override void Spawned()
    {
        rb = GetComponent<Rigidbody>();
        knockbackReceiver = GetComponent<KnockbackReceiver>();
        batAttack = GetComponent<BatAttack>();
        
        if (HasInputAuthority)
        {
            CameraFollow cameraFollow = Camera.main.GetComponent<CameraFollow>();
            if (cameraFollow != null)
                cameraFollow.target = transform;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!GetInput(out BbangmangiInputData input))
            return;

        if (knockbackReceiver != null && knockbackReceiver.IsStunned)
            return;

        Vector2 moveInput = input.MoveDirection;

        if (moveInput.sqrMagnitude > 1f)
            moveInput.Normalize();

        Vector3 move = new Vector3(moveInput.x, 0, moveInput.y);

        verticalVelocity += gravity * Runner.DeltaTime;

        Vector3 velocity = new Vector3(
            move.x * moveSpeed,
            verticalVelocity,
            move.z * moveSpeed
        );

        transform.position += velocity * Runner.DeltaTime;

        if (move.sqrMagnitude > 0.001f)
            transform.forward = move;

        if (input.AttackPressed)
        {
            Vector2 attackInput = input.AttackDirection;

            if (attackInput.sqrMagnitude > 0.001f)
            {
                Vector3 attackDir = new Vector3(attackInput.x, 0, attackInput.y);
                transform.forward = attackDir.normalized;
            }

            batAttack.Attack();
        }
        
        if (Physics.Raycast(
                transform.position,
                Vector3.down,
                out RaycastHit hit,
                groundCheckDistance,
                groundLayer))
        {
            if (verticalVelocity < 0)
            {
                verticalVelocity = 0;

                Vector3 position = transform.position;
                position.y = hit.point.y + 1f;
                transform.position = position;
            }
        }
    }
}