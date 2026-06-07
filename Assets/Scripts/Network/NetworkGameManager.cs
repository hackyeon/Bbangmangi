using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class NetworkGameManager : MonoBehaviour
{
    public static NetworkGameManager Instance { get; private set; }
    
    public CharacterData[] characters;
    public NetworkObject playerPrefab;
    public float spawnHeight = 20f;

    private NetworkRunner runner;
    private readonly Dictionary<PlayerRef, NetworkObject> spawnedPlayers = new();
    private int nextJoinOrder = 1;
    
    private void Awake()
    {
        Instance = this;
    }

    public void Initialize(NetworkRunner networkRunner)
    {
        runner = networkRunner;
    }
    
    public void RebuildSpawnedPlayers(NetworkRunner networkRunner)
    {
        runner = networkRunner;
        spawnedPlayers.Clear();

        NetworkPlayerName[] players =
            FindObjectsByType<NetworkPlayerName>(FindObjectsSortMode.None);

        foreach (NetworkPlayerName player in players)
        {
            if (player == null || player.Object == null)
                continue;

            spawnedPlayers[player.Object.InputAuthority] = player.Object;
        }
    }
    
    public int NextJoinOrder()
    {
        if (runner == null || !runner.IsServer)
            return 0;

        int maxJoinOrder = 0;

        NetworkPlayerCommand[] commands =
            FindObjectsByType<NetworkPlayerCommand>(FindObjectsSortMode.None);

        foreach (NetworkPlayerCommand command in commands)
        {
            if (command == null || command.Object == null)
                continue;

            maxJoinOrder = Mathf.Max(maxJoinOrder, command.JoinOrder);
        }

        if (nextJoinOrder <= maxJoinOrder)
            nextJoinOrder = maxJoinOrder + 1;

        return nextJoinOrder++;
    }

    public void RequestSpawn(
        PlayerRef player,
        string nickname,
        int characterId,
        string connectionId
    )
    {
        if (runner == null || !runner.IsRunning)
            return;

        if (!runner.IsServer)
            return;

        CharacterData character = FindCharacter(characterId);

        if (character == null)
            return;

        SpawnPlayer(player, nickname, character, connectionId);
    }
    
    private CharacterData FindCharacter(int characterId)
    {
        foreach (CharacterData character in characters)
        {
            if (character != null && character.id == characterId)
                return character;
        }

        return null;
    }

    private void SpawnPlayer(
        PlayerRef player,
        string nickname,
        CharacterData character,
        string connectionId
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
        {
            stats.SetConnectionId(connectionId);
            stats.Apply(character);
        }
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
