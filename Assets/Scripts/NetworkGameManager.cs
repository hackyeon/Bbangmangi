using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class NetworkGameManager : MonoBehaviour
{
    public static NetworkGameManager Instance { get; private set; }

    public NetworkObject playerPrefab;
    public float spawnHeight = 20f;

    private NetworkRunner runner;
    private readonly Dictionary<PlayerRef, NetworkObject> spawnedPlayers = new();

    private void Awake()
    {
        Instance = this;
    }

    public void Initialize(NetworkRunner networkRunner)
    {
        runner = networkRunner;
    }

    public void RequestSpawn(
        PlayerRef player,
        string nickname,
        CharacterType characterType
    )
    {
        if (runner == null || !runner.IsRunning)
        {
            Debug.LogError("[NetworkGameManager] Runner is not ready");
            return;
        }

        if (!runner.IsServer)
        {
            Debug.LogWarning("[NetworkGameManager] Only server can spawn");
            return;
        }

        SpawnPlayer(player, nickname, characterType);
    }

    private void SpawnPlayer(
        PlayerRef player,
        string nickname,
        CharacterType characterType
    )
    {
        if (spawnedPlayers.ContainsKey(player))
            return;

        NetworkObject playerObject = runner.Spawn(
            playerPrefab,
            new Vector3(0, spawnHeight, 0),
            Quaternion.identity,
            player
        );

        spawnedPlayers[player] = playerObject;

        NetworkPlayerName playerName =
            playerObject.GetComponent<NetworkPlayerName>();

        if (playerName != null)
            playerName.SetNickname(nickname);

        NetworkPlayerStats stats =
            playerObject.GetComponent<NetworkPlayerStats>();

        if (stats != null)
            stats.Apply(characterType);
    }

    public void DespawnPlayer(PlayerRef player)
    {
        if (runner == null || !runner.IsServer)
            return;

        if (spawnedPlayers.TryGetValue(player, out NetworkObject playerObject))
        {
            if (playerObject != null)
                runner.Despawn(playerObject);

            spawnedPlayers.Remove(player);
        }
    }
}