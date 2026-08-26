using Fusion;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(NetworkObject), typeof(Rigidbody))]
public class NetworkItemPickup : NetworkBehaviour
{
    [SerializeField] private BattleItemType itemType;

    [Networked]
    public NetworkBool IsCollected { get; private set; }

    [Networked]
    private NetworkBool HasSpawnPose { get; set; }

    [Networked]
    private Vector3 SpawnPosition { get; set; }

    [Networked]
    private Quaternion SpawnRotation { get; set; }

    public BattleItemType ItemType => itemType;

    private void Awake()
    {
        ConfigureRigidbody();
    }

    public override void Spawned()
    {
        ApplySpawnPose();

        Collider itemCollider = GetComponentInChildren<Collider>();

        if (itemCollider == null)
        {
            Debug.LogWarning(
                $"[Item] {name}에 Pickup Collider가 없습니다.",
                this
            );
        }
        else if (!itemCollider.isTrigger)
        {
            Debug.LogWarning(
                $"[Item] {name}의 Pickup Collider에서 Is Trigger를 켜주세요.",
                this
            );
        }
    }

    public override void Render()
    {
        ApplySpawnPose();
    }

    public void InitializeSpawnPose(Vector3 position, Quaternion rotation)
    {
        if (!HasStateAuthority)
            return;

        SpawnPosition = position;
        SpawnRotation = rotation;
        HasSpawnPose = true;
        transform.SetPositionAndRotation(position, rotation);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!HasStateAuthority || IsCollected)
            return;

        NetworkRoundManager roundManager = NetworkRoundManager.Instance;

        if (roundManager == null ||
            roundManager.Runner != Runner ||
            roundManager.Phase != RoundPhase.Playing)
        {
            return;
        }

        NetworkPlayerStats collector =
            other.GetComponentInParent<NetworkPlayerStats>();

        if (!IsValidCollector(collector))
            return;

        IsCollected = true;

        NetworkCharacterItemEffect itemEffect =
            collector.GetComponent<NetworkCharacterItemEffect>();

        bool effectApplied =
            itemEffect != null && itemEffect.TryApplyItem(itemType);

        HandleItemCollected(collector, effectApplied);

        NetworkItemSpawner itemSpawner =
            NetworkItemSpawner.FindForRunner(Runner);

        itemSpawner?.NotifyItemCollected(Object);

        if (Object != null && Object.IsValid)
            Runner.Despawn(Object);
    }

    private bool IsValidCollector(NetworkPlayerStats collector)
    {
        return collector != null &&
               collector.Object != null &&
               collector.Object.IsValid &&
               collector.Runner == Runner &&
               collector.GetComponent<NetworkPlayerMotor>() != null;
    }

    private void HandleItemCollected(
        NetworkPlayerStats collector,
        bool effectApplied
    )
    {
        NetworkPlayerName playerName =
            collector.GetComponent<NetworkPlayerName>();

        string collectorName = playerName != null
            ? playerName.Nickname.ToString()
            : "";

        if (string.IsNullOrEmpty(collectorName))
        {
            collectorName = collector.IsBot
                ? "Bot"
                : $"Player {collector.Object.InputAuthority.PlayerId}";
        }

        if (effectApplied)
        {
            Debug.Log($"[Item] {collectorName} collected {itemType}");
            return;
        }

        Debug.LogWarning(
            $"[Item] {collectorName} could not apply {itemType}. " +
            "NetworkCharacterItemEffect를 확인해 주세요.",
            collector
        );
    }

    private void Reset()
    {
        ConfigureRigidbody();
        ConfigureCollider();
    }

    private void OnValidate()
    {
        ConfigureRigidbody();
        ConfigureCollider();
    }

    private void ConfigureRigidbody()
    {
        Rigidbody itemRigidbody = GetComponent<Rigidbody>();

        if (itemRigidbody == null)
            return;

        itemRigidbody.useGravity = false;
        itemRigidbody.isKinematic = true;
    }

    private void ConfigureCollider()
    {
        Collider itemCollider = GetComponentInChildren<Collider>();

        if (itemCollider != null)
            itemCollider.isTrigger = true;
    }

    private void ApplySpawnPose()
    {
        if (!HasSpawnPose)
            return;

        transform.SetPositionAndRotation(SpawnPosition, SpawnRotation);
    }
}
