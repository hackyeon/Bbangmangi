using Fusion;
using UnityEngine;

public class NetworkPlayerMotor : NetworkBehaviour
{
    public float moveSpeed = 6f;
    public float gravity = -25f;
    public float groundCheckDistance = 1.2f;
    public float capsuleHalfHeight = 1f;
    public LayerMask groundLayer;

    private float verticalVelocity;
    private KnockbackReceiver knockbackReceiver;
    private BatAttack batAttack;

    public override void Spawned()
    {
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

        Vector2 moveInput = input.MoveDirection;

        if (moveInput.sqrMagnitude > 1f)
            moveInput.Normalize();

        Vector3 move = new Vector3(moveInput.x, 0f, moveInput.y);

        verticalVelocity += gravity * Runner.DeltaTime;

        Vector3 velocity =
            move * moveSpeed +
            Vector3.up * verticalVelocity;

        if (knockbackReceiver != null)
        {
            velocity += knockbackReceiver.ConsumeVelocity(Runner.DeltaTime);
        }

        transform.position += velocity * Runner.DeltaTime;

        GroundCheck();

        if (move.sqrMagnitude > 0.001f)
            transform.forward = move;

        if (input.AttackPressed && batAttack != null)
            batAttack.Attack();
    }

    private void GroundCheck()
    {
        if (Physics.Raycast(
                transform.position,
                Vector3.down,
                out RaycastHit hit,
                groundCheckDistance,
                groundLayer))
        {
            if (verticalVelocity < 0f)
            {
                verticalVelocity = 0f;

                Vector3 position = transform.position;
                position.y = hit.point.y + capsuleHalfHeight;
                transform.position = position;
            }
        }
    }
}