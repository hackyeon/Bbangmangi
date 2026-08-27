using Fusion;
using UnityEngine;

[DisallowMultipleComponent]
public class NetworkBotAI : MonoBehaviour
{
    [SerializeField] private float targetSearchRange = 18f;
    [SerializeField] private float attackDistance = 2.8f;
    [SerializeField] private float stopDistance = 1.6f;
    [SerializeField] private float returnToCenterDistance = 16f;
    [SerializeField] private float targetRefreshInterval = 0.35f;
    [SerializeField] private float wanderChangeInterval = 1.4f;
    [SerializeField] private float revengeDuration = 3f;
    [SerializeField] private float ledgeLookAheadDistance = 1.2f;
    [SerializeField] private float ledgeRayStartHeight = 0.6f;
    [SerializeField] private float ledgeRayExtraDepth = 1.2f;

    private NetworkObject networkObject;
    private NetworkPlayerStats stats;
    private NetworkPlayerMotor motor;
    private BatAttack attack;
    private KnockbackReceiver knockbackReceiver;
    private Transform target;
    private Vector2 wanderDirection;
    private float nextTargetRefreshTime;
    private float nextWanderChangeTime;

    private void Awake()
    {
        SetupReferences();
        PickWanderDirection();
    }

    public void ServerTick(float deltaTime)
    {
        SetupReferences();

        if (networkObject == null ||
            !networkObject.IsValid ||
            !networkObject.HasStateAuthority)
        {
            return;
        }

        if (stats == null || !stats.IsBot || motor == null)
            return;

        if (knockbackReceiver != null && knockbackReceiver.IsStunned)
        {
            motor.SetBotInput(Vector2.zero, false, transform.forward);
            return;
        }

        if (Time.time >= nextTargetRefreshTime || !IsValidTarget(target))
        {
            target = FindTarget();
            nextTargetRefreshTime = Time.time + targetRefreshInterval;
        }

        if (target != null)
        {
            ChaseTarget();
            return;
        }

        Wander(deltaTime);
    }

    private void ChaseTarget()
    {
        Vector3 toTarget = target.position - transform.position;
        toTarget.y = 0f;

        float distance = toTarget.magnitude;
        Vector3 lookDirection =
            distance > 0.001f ? toTarget.normalized : transform.forward;

        Vector2 moveInput = Vector2.zero;

        if (distance > stopDistance)
            moveInput = new Vector2(lookDirection.x, lookDirection.z);

        bool shouldAttack = attack != null && distance <= attackDistance;
        SetSafeBotInput(moveInput, shouldAttack, lookDirection);
    }

    private void Wander(float deltaTime)
    {
        Vector3 position = transform.position;
        Vector3 mapCenter = GetMapCenter();
        Vector3 fromCenter = position - mapCenter;
        fromCenter.y = 0f;

        if (fromCenter.magnitude >= GetReturnToCenterDistance())
        {
            Vector3 toCenter = -fromCenter.normalized;
            motor.SetBotInput(
                new Vector2(toCenter.x, toCenter.z),
                false,
                toCenter
            );
            return;
        }

        if (Time.time >= nextWanderChangeTime)
            PickWanderDirection();

        Vector3 lookDirection =
            new Vector3(wanderDirection.x, 0f, wanderDirection.y);

        SetSafeBotInput(wanderDirection, false, lookDirection);
    }

    private Transform FindTarget()
    {
        Transform revengeTarget = FindRevengeTarget();

        if (revengeTarget != null)
            return revengeTarget;

        NetworkPlayerStats[] players =
            FindObjectsByType<NetworkPlayerStats>(FindObjectsSortMode.None);

        NetworkPlayerStats closestCharacter = null;
        float closestCharacterDistance = float.MaxValue;
        float targetSearchSqr = targetSearchRange * targetSearchRange;

        foreach (NetworkPlayerStats candidate in players)
        {
            if (!IsValidCandidate(candidate))
                continue;

            float sqrDistance =
                FlatSqrDistance(candidate.transform.position, transform.position);

            if (sqrDistance <= targetSearchSqr &&
                sqrDistance < closestCharacterDistance)
            {
                closestCharacterDistance = sqrDistance;
                closestCharacter = candidate;
            }
        }

        return closestCharacter != null ? closestCharacter.transform : null;
    }

    private Transform FindRevengeTarget()
    {
        if (knockbackReceiver == null)
            return null;

        NetworkObject attackerObject = knockbackReceiver.LastAttackerObject;

        if (attackerObject == null ||
            !attackerObject.IsValid ||
            attackerObject == networkObject)
        {
            return null;
        }

        if (knockbackReceiver.LastHitAge > revengeDuration)
            return null;

        NetworkPlayerStats attackerStats =
            attackerObject.GetComponent<NetworkPlayerStats>();

        if (!IsValidCandidate(attackerStats))
            return null;

        if (FlatSqrDistance(attackerObject.transform.position, transform.position) >
            targetSearchRange * targetSearchRange)
        {
            return null;
        }

        return attackerObject.transform;
    }

    private bool IsValidTarget(Transform targetTransform)
    {
        if (targetTransform == null)
            return false;

        NetworkPlayerStats targetStats =
            targetTransform.GetComponent<NetworkPlayerStats>();

        return IsValidCandidate(targetStats);
    }

    private bool IsValidCandidate(NetworkPlayerStats candidate)
    {
        if (candidate == null || candidate.Object == null)
            return false;

        if (!candidate.Object.IsValid)
            return false;

        if (networkObject == null || candidate.Runner != networkObject.Runner)
            return false;

        if (candidate.Object == networkObject)
            return false;

        NetworkPlayerMotor candidateMotor =
            candidate.GetComponent<NetworkPlayerMotor>();

        if (candidateMotor != null && !candidateMotor.IsGrounded)
            return false;

        return true;
    }

    private void SetSafeBotInput(
        Vector2 moveInput,
        bool attackPressed,
        Vector3 lookDirection
    )
    {
        if (moveInput.sqrMagnitude <= 0.001f || IsMoveDirectionSafe(moveInput))
        {
            motor.SetBotInput(moveInput, attackPressed, lookDirection);
            return;
        }

        target = null;

        Vector2 fallbackInput = GetFallbackMoveInput(moveInput);
        Vector3 fallbackLookDirection =
            fallbackInput.sqrMagnitude > 0.001f
                ? new Vector3(fallbackInput.x, 0f, fallbackInput.y)
                : lookDirection;

        motor.SetBotInput(fallbackInput, false, fallbackLookDirection);
    }

    private bool IsMoveDirectionSafe(Vector2 moveInput)
    {
        if (moveInput.sqrMagnitude <= 0.001f)
            return true;

        Vector3 moveDirection =
            new Vector3(moveInput.x, 0f, moveInput.y).normalized;

        return HasGroundAhead(moveDirection);
    }

    private bool HasGroundAhead(Vector3 moveDirection)
    {
        if (moveDirection.sqrMagnitude <= 0.001f)
            return true;

        LayerMask groundMask =
            motor != null && motor.groundLayer.value != 0
                ? motor.groundLayer
                : ~0;

        Vector3 rayOrigin =
            transform.position +
            moveDirection.normalized * ledgeLookAheadDistance +
            Vector3.up * ledgeRayStartHeight;

        float rayDistance =
            (motor != null ? motor.capsuleHalfHeight : 1f) +
            ledgeRayStartHeight +
            ledgeRayExtraDepth;

        return Physics.Raycast(
            rayOrigin,
            Vector3.down,
            rayDistance,
            groundMask,
            QueryTriggerInteraction.Ignore
        );
    }

    private Vector2 GetFallbackMoveInput(Vector2 blockedMoveInput)
    {
        Vector3 blockedDirection =
            new Vector3(blockedMoveInput.x, 0f, blockedMoveInput.y).normalized;

        if (TryGetSafeMoveInput(-blockedDirection, out Vector2 fallbackInput))
            return fallbackInput;

        Vector3 toCenter = GetMapCenter() - transform.position;
        toCenter.y = 0f;

        if (TryGetSafeMoveInput(toCenter, out fallbackInput))
            return fallbackInput;

        if (TryGetSafeMoveInput(transform.right, out fallbackInput))
            return fallbackInput;

        if (TryGetSafeMoveInput(-transform.right, out fallbackInput))
            return fallbackInput;

        return Vector2.zero;
    }

    private Vector3 GetMapCenter()
    {
        MapShrinkController mapShrinkController =
            MapShrinkController.Instance;

        if (mapShrinkController != null)
            return mapShrinkController.PlayAreaCenter;

        return Vector3.zero;
    }

    private float GetReturnToCenterDistance()
    {
        MapShrinkController mapShrinkController =
            MapShrinkController.Instance;

        if (mapShrinkController == null)
            return returnToCenterDistance;

        float currentSafeRadius =
            mapShrinkController.GetSafeHorizontalRadius(
                ledgeLookAheadDistance
            );

        if (currentSafeRadius <= 0.01f)
            return returnToCenterDistance;

        return Mathf.Min(returnToCenterDistance, currentSafeRadius);
    }

    private bool TryGetSafeMoveInput(Vector3 direction, out Vector2 moveInput)
    {
        moveInput = Vector2.zero;

        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
            return false;

        direction.Normalize();

        if (!HasGroundAhead(direction))
            return false;

        moveInput = new Vector2(direction.x, direction.z);
        return true;
    }

    private static float FlatSqrDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return (a - b).sqrMagnitude;
    }

    private void PickWanderDirection()
    {
        wanderDirection = Random.insideUnitCircle.normalized;

        if (wanderDirection.sqrMagnitude < 0.001f)
            wanderDirection = Vector2.up;

        nextWanderChangeTime = Time.time + wanderChangeInterval;
    }

    private void SetupReferences()
    {
        if (networkObject == null)
            networkObject = GetComponent<NetworkObject>();

        if (stats == null)
            stats = GetComponent<NetworkPlayerStats>();

        if (motor == null)
            motor = GetComponent<NetworkPlayerMotor>();

        if (attack == null)
            attack = GetComponent<BatAttack>();

        if (knockbackReceiver == null)
            knockbackReceiver = GetComponent<KnockbackReceiver>();
    }
}
