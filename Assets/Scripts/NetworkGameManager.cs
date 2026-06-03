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
        int characterId
    )
    {
        if (runner == null || !runner.IsRunning)
            return;

        if (!runner.IsServer)
            return;

        CharacterData character = FindCharacter(characterId);

        if (character == null)
            return;

        SpawnPlayer(player, nickname, character);
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
        CharacterData character
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
            stats.Apply(character);
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