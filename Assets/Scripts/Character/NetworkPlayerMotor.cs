using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class NetworkPlayerMotor : NetworkBehaviour
{
    public float moveSpeed = 6f;
    public float gravity = -25f;
    public float groundCheckDistance = 1.2f;
    public float capsuleHalfHeight = 1f;
    public float playerRadius = 0.5f;
    public LayerMask groundLayer;
    public bool IsGrounded { get; private set; }
    public Vector2 CurrentMoveInput { get; private set; }
    
    private float verticalVelocity;
    private KnockbackReceiver knockbackReceiver;
    private BatAttack batAttack;
    private Vector2 botMoveInput;
    private Vector3 botLookDirection;
    private bool botAttackPressed;
    private readonly Collider[] overlapBuffer = new Collider[64];
    private readonly HashSet<NetworkPlayerMotor> overlapPlayers = new();

    public override void Spawned()
    {
        knockbackReceiver = GetComponent<KnockbackReceiver>();
        batAttack = GetComponent<BatAttack>();

        if (HasInputAuthority && BelongsToLocalConnection())
        {
            CameraFollow cameraFollow = FindCameraFollow();
            if (cameraFollow != null)
                cameraFollow.target = transform;

            if (NetworkRoundManager.IsGameplayActive)
            {
                CharacterSelectUI characterSelectUI =
                    FindFirstObjectByType<CharacterSelectUI>();

                characterSelectUI?.Hide();
            }
        }
    }
    
    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (NetworkRunnerManager.Instance != null &&
            NetworkRunnerManager.Instance.IsHostMigrating)
        {
            return;
        }
        
        if (Object.InputAuthority == runner.LocalPlayer)
        {
            CharacterSelectUI ui = FindFirstObjectByType<CharacterSelectUI>();

            if (ui != null)
            {
                ui.Show();
                ui.SetStartButtonEnabled(true);
            }

            CameraFollow cameraFollow = FindCameraFollow();
            if (cameraFollow != null)
                cameraFollow.target = null;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority)
            return;

        Vector2 moveInput = Vector2.zero;
        bool attackPressed = false;
        bool isGameplayActive = NetworkRoundManager.IsGameplayActive;

        if (!isGameplayActive)
        {
            botMoveInput = Vector2.zero;
            botAttackPressed = false;
            botLookDirection = Vector3.zero;
            CurrentMoveInput = Vector2.zero;
            knockbackReceiver?.ConsumeVelocity(Runner.DeltaTime);
            return;
        }

        if (IsBotControlled())
        {
            moveInput = botMoveInput;
            attackPressed = botAttackPressed;
            botAttackPressed = false;
        }
        else if (GetInput(out BbangmangiInputData input))
        {
            moveInput = input.MoveDirection;
            attackPressed = input.AttackPressed;
        }

        if (moveInput.sqrMagnitude > 1f)
            moveInput.Normalize();

        if (knockbackReceiver != null && knockbackReceiver.IsStunned)
        {
            moveInput = Vector2.zero;
            attackPressed = false;
        }

        CurrentMoveInput = moveInput;

        Vector3 move = new Vector3(moveInput.x, 0f, moveInput.y);

        verticalVelocity += gravity * Runner.DeltaTime;

        Vector3 velocity =
            move * moveSpeed +
            Vector3.up * verticalVelocity;

        if (knockbackReceiver != null)
            velocity += knockbackReceiver.ConsumeVelocity(Runner.DeltaTime);

        transform.position += velocity * Runner.DeltaTime;

        GroundCheck();

        ResolvePlayerOverlap();
        
        if (move.sqrMagnitude > 0.001f)
            transform.forward = move;
        else if (botLookDirection.sqrMagnitude > 0.001f)
            transform.forward = botLookDirection.normalized;

        if (attackPressed && batAttack != null)
            batAttack.Attack();
    }

    public void SetBotInput(
        Vector2 moveDirection,
        bool attackPressed,
        Vector3 lookDirection
    )
    {
        if (!HasStateAuthority)
            return;

        if (!IsBotControlled())
            return;

        if (!NetworkRoundManager.IsGameplayActive)
        {
            botMoveInput = Vector2.zero;
            botAttackPressed = false;
            botLookDirection = Vector3.zero;
            return;
        }

        if (moveDirection.sqrMagnitude > 1f)
            moveDirection.Normalize();

        lookDirection.y = 0f;

        botMoveInput = moveDirection;
        botAttackPressed |= attackPressed;
        botLookDirection = lookDirection;
    }

    public void RespawnAt(Vector3 position, Quaternion rotation)
    {
        if (!HasStateAuthority)
            return;

        verticalVelocity = 0f;
        botMoveInput = Vector2.zero;
        botLookDirection = Vector3.zero;
        botAttackPressed = false;
        CurrentMoveInput = Vector2.zero;
        IsGrounded = false;

        NetworkTransform networkTransform = GetComponent<NetworkTransform>();

        if (networkTransform != null)
            networkTransform.Teleport(position, rotation);
        else
            transform.SetPositionAndRotation(position, rotation);
    }
    
    private void ResolvePlayerOverlap()
    {
        overlapPlayers.Clear();

        int hitCount = Physics.OverlapSphereNonAlloc(
            transform.position,
            playerRadius,
            overlapBuffer,
            ~0,
            QueryTriggerInteraction.Ignore
        );

        for (int hitIndex = 0; hitIndex < hitCount; hitIndex++)
        {
            Collider hit = overlapBuffer[hitIndex];

            if (hit == null)
                continue;

            if (hit.gameObject == gameObject)
                continue;

            NetworkPlayerMotor other =
                hit.GetComponentInParent<NetworkPlayerMotor>();

            if (other == null ||
                other == this ||
                !overlapPlayers.Add(other))
            {
                continue;
            }

            Vector3 dir =
                transform.position -
                other.transform.position;

            dir.y = 0f;

            float distance = dir.magnitude;

            float minDistance =
                playerRadius * 2f;

            if (distance < 0.001f)
            {
                dir = transform.forward;
                distance = 0.001f;
            }

            if (distance < minDistance)
            {
                Vector3 push =
                    dir.normalized *
                    ((minDistance - distance) * 0.5f);

                transform.position += push;
            }
        }
    }

    private bool BelongsToLocalConnection()
    {
        NetworkPlayerStats stats = GetComponent<NetworkPlayerStats>();

        if (stats == null)
            return true;

        string connectionId = stats.ConnectionId.ToString();

        return string.IsNullOrEmpty(connectionId) ||
               connectionId == NetworkRunnerManager.LocalConnectionId;
    }

    private bool IsBotControlled()
    {
        NetworkPlayerStats stats = GetComponent<NetworkPlayerStats>();
        return stats != null && stats.IsBot;
    }

    private static CameraFollow FindCameraFollow()
    {
        Camera mainCamera = Camera.main;

        if (mainCamera == null)
            return null;

        return mainCamera.GetComponent<CameraFollow>();
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
            IsGrounded = true;
            
            if (verticalVelocity < 0f)
            {
                verticalVelocity = 0f;

                Vector3 position = transform.position;
                position.y = hit.point.y + capsuleHalfHeight;
                transform.position = position;
            }
        }
        else
        {
            IsGrounded = false;
        }
    }
}
