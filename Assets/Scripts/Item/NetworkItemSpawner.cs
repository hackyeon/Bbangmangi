using Fusion;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(NetworkRoundManager))]
public class NetworkItemSpawner : NetworkBehaviour
{
    [Header("Spawn Schedule")]
    [SerializeField, Min(0.1f)] private float itemSpawnInterval = 30f;

    [Header("Network Item Prefabs")]
    [SerializeField] private NetworkObject giantHammerItemPrefab;
    [SerializeField] private NetworkObject shieldItemPrefab;
    [SerializeField] private NetworkObject bombItemPrefab;

    [Networked]
    public TickTimer ItemSpawnTimer { get; private set; }

    [Networked]
    public NetworkId ActiveItemId { get; private set; }

    private NetworkRoundManager roundManager;
    private ItemSpawnPoint itemSpawnPoint;
    private bool hasWarnedMissingSpawnPoint;
    private bool hasWarnedMissingPrefabs;

    public override void Spawned()
    {
        roundManager = GetComponent<NetworkRoundManager>();

        if (HasStateAuthority)
            FindSpawnPoint();
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority || !IsPlaying())
            return;

        if (!ItemSpawnTimer.Expired(Runner))
            return;

        ItemSpawnTimer = TickTimer.CreateFromSeconds(
            Runner,
            itemSpawnInterval
        );

        ReplaceActiveItem();
    }

    public void HandleRoundPhaseStarted(RoundPhase phase)
    {
        if (!HasStateAuthority)
            return;

        DespawnAllItems();

        if (phase == RoundPhase.Playing)
        {
            ItemSpawnTimer = TickTimer.CreateFromSeconds(
                Runner,
                itemSpawnInterval
            );
        }
        else
        {
            ItemSpawnTimer = TickTimer.None;
        }
    }

    public void NotifyItemCollected(NetworkObject collectedItem)
    {
        if (!HasStateAuthority || collectedItem == null)
            return;

        if (ActiveItemId == collectedItem.Id)
            ActiveItemId = default;
    }

    public void ValidateAfterHostMigration()
    {
        if (!HasStateAuthority)
            return;

        if (!IsPlaying())
        {
            ItemSpawnTimer = TickTimer.None;
            DespawnAllItems();
            return;
        }

        NetworkItemPickup itemToKeep = ResolveActiveItem();
        bool hasResolvedTrackedItem = itemToKeep != null;
        NetworkItemPickup[] items = FindItemsForRunner();

        foreach (NetworkItemPickup item in items)
        {
            if (!IsUsableItem(item))
                continue;

            if (item.IsCollected)
            {
                Runner.Despawn(item.Object);
                continue;
            }

            if (!hasResolvedTrackedItem &&
                (itemToKeep == null ||
                 item.Object.Id.CompareTo(itemToKeep.Object.Id) < 0))
            {
                itemToKeep = item;
            }
        }

        ActiveItemId = itemToKeep != null
            ? itemToKeep.Object.Id
            : default;

        foreach (NetworkItemPickup item in items)
        {
            if (IsUsableItem(item) && item != itemToKeep)
                Runner.Despawn(item.Object);
        }

        if (!ItemSpawnTimer.IsRunning)
        {
            Debug.LogWarning(
                "[Item] 복원된 Spawn Timer가 없어 30초 주기로 복구합니다.",
                this
            );

            ItemSpawnTimer = TickTimer.CreateFromSeconds(
                Runner,
                itemSpawnInterval
            );
        }
    }

    public static NetworkItemSpawner FindForRunner(NetworkRunner targetRunner)
    {
        if (targetRunner == null)
            return null;

        NetworkItemSpawner[] spawners =
            FindObjectsByType<NetworkItemSpawner>(FindObjectsSortMode.None);

        foreach (NetworkItemSpawner spawner in spawners)
        {
            if (spawner != null &&
                spawner.Object != null &&
                spawner.Object.IsValid &&
                spawner.Runner == targetRunner)
            {
                return spawner;
            }
        }

        return null;
    }

    private void ReplaceActiveItem()
    {
        DespawnAllItems();

        NetworkObject itemPrefab = SelectRandomItemPrefab();

        if (itemPrefab == null)
            return;

        if (!FindSpawnPoint())
            return;

        Vector3 spawnPosition = itemSpawnPoint.transform.position;
        Quaternion spawnRotation = itemSpawnPoint.transform.rotation;

        NetworkObject spawnedItem = Runner.Spawn(
            itemPrefab,
            spawnPosition,
            spawnRotation,
            PlayerRef.None,
            onBeforeSpawned: (_, itemObject) =>
            {
                NetworkItemPickup pickup =
                    itemObject.GetComponent<NetworkItemPickup>();

                pickup?.InitializeSpawnPose(spawnPosition, spawnRotation);
            }
        );

        if (spawnedItem == null)
            return;

        NetworkItemPickup pickup =
            spawnedItem.GetComponent<NetworkItemPickup>();

        if (pickup == null)
        {
            Debug.LogError(
                $"[Item] {itemPrefab.name}에 NetworkItemPickup이 없습니다.",
                itemPrefab
            );

            if (spawnedItem.IsValid)
                Runner.Despawn(spawnedItem);

            return;
        }

        ActiveItemId = spawnedItem.Id;
        Debug.Log($"[Item] Spawned {pickup.ItemType}");
    }

    private NetworkObject SelectRandomItemPrefab()
    {
        int configuredPrefabCount = 0;

        if (giantHammerItemPrefab != null)
            configuredPrefabCount++;

        if (shieldItemPrefab != null)
            configuredPrefabCount++;

        if (bombItemPrefab != null)
            configuredPrefabCount++;

        if (configuredPrefabCount == 0)
        {
            if (!hasWarnedMissingPrefabs)
            {
                hasWarnedMissingPrefabs = true;
                Debug.LogWarning(
                    "[Item] Network Item Prefab이 연결되지 않았습니다.",
                    this
                );
            }

            return null;
        }

        int selectedIndex = Random.Range(0, configuredPrefabCount);

        if (giantHammerItemPrefab != null && selectedIndex-- == 0)
            return giantHammerItemPrefab;

        if (shieldItemPrefab != null && selectedIndex-- == 0)
            return shieldItemPrefab;

        return bombItemPrefab;
    }

    private void DespawnAllItems()
    {
        ActiveItemId = default;

        foreach (NetworkItemPickup item in FindItemsForRunner())
        {
            if (IsUsableItem(item))
                Runner.Despawn(item.Object);
        }
    }

    private NetworkItemPickup ResolveActiveItem()
    {
        if (!ActiveItemId.IsValid ||
            !Runner.TryFindObject(ActiveItemId, out NetworkObject itemObject) ||
            itemObject == null ||
            !itemObject.IsValid)
        {
            ActiveItemId = default;
            return null;
        }

        NetworkItemPickup pickup =
            itemObject.GetComponent<NetworkItemPickup>();

        if (pickup == null || pickup.IsCollected)
        {
            ActiveItemId = default;
            return null;
        }

        return pickup;
    }

    private NetworkItemPickup[] FindItemsForRunner()
    {
        return FindObjectsByType<NetworkItemPickup>(
            FindObjectsSortMode.None
        );
    }

    private bool IsUsableItem(NetworkItemPickup item)
    {
        return item != null &&
               item.Object != null &&
               item.Object.IsValid &&
               item.Runner == Runner;
    }

    private bool FindSpawnPoint()
    {
        if (itemSpawnPoint != null)
            return true;

        itemSpawnPoint = FindFirstObjectByType<ItemSpawnPoint>();

        if (itemSpawnPoint != null)
            return true;

        if (!hasWarnedMissingSpawnPoint)
        {
            hasWarnedMissingSpawnPoint = true;
            Debug.LogWarning("[Item] ItemSpawnPoint was not found.", this);
        }

        return false;
    }

    private bool IsPlaying()
    {
        if (roundManager == null)
            roundManager = GetComponent<NetworkRoundManager>();

        return roundManager != null &&
               roundManager.Phase == RoundPhase.Playing;
    }
}
