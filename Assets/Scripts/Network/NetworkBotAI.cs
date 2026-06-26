using Fusion;
using UnityEngine;

[DisallowMultipleComponent]
public class NetworkBotAI : MonoBehaviour
{
    [SerializeField] private float targetSearchRange = 18f;
    [SerializeField] private float userPriorityRange = 12f;
    [SerializeField] private float attackDistance = 2.8f;
    [SerializeField] private float stopDistance = 1.6f;
    [SerializeField] private float returnToCenterDistance = 16f;
    [SerializeField] private float targetRefreshInterval = 0.35f;
    [SerializeField] private float wanderChangeInterval = 1.4f;
    [SerializeField] private float revengeDuration = 3f;

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
        motor.SetBotInput(moveInput, shouldAttack, lookDirection);
    }

    private void Wander(float deltaTime)
    {
        Vector3 position = transform.position;
        Vector3 fromCenter = new Vector3(position.x, 0f, position.z);

        if (fromCenter.magnitude >= returnToCenterDistance)
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

        motor.SetBotInput(wanderDirection, false, lookDirection);
    }

    private Transform FindTarget()
    {
        Transform revengeTarget = FindRevengeTarget();

        if (revengeTarget != null)
            return revengeTarget;

        NetworkPlayerStats[] players =
            FindObjectsByType<NetworkPlayerStats>(FindObjectsSortMode.None);

        NetworkPlayerStats closestUser = null;
        NetworkPlayerStats closestCharacter = null;
        float closestUserDistance = float.MaxValue;
        float closestCharacterDistance = float.MaxValue;
        float userPrioritySqr = userPriorityRange * userPriorityRange;
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

            if (!candidate.IsBot &&
                sqrDistance <= userPrioritySqr &&
                sqrDistance < closestUserDistance)
            {
                closestUserDistance = sqrDistance;
                closestUser = candidate;
            }
        }

        if (closestUser != null)
            return closestUser.transform;

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

        if (candidate.Object == networkObject)
            return false;

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
