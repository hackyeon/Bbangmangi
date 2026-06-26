using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class NetworkGameManager : MonoBehaviour
{
    public static NetworkGameManager Instance { get; private set; }

    [Header("Players")]
    public CharacterData[] characters;
    public NetworkObject playerPrefab;
    public float spawnHeight = 20f;

    [Header("Bots")]
    [SerializeField, Min(0)] private int targetCharacterCount = 6;
    [SerializeField, Min(0)] private int minimumBotCount = 2;
    [SerializeField, Min(0.1f)] private float botCheckInterval = 1f;
    [SerializeField, Min(0f)] private float botRespawnDelay = 2f;
    [SerializeField, Min(1f)] private float botSpawnRadius = 8f;
    [SerializeField]
    private string[] botNames =
    {
        "망치왕",
        "쫄보망치",
        "분노한빵",
        "한방맨",
        "빵야빵야",
        "도망자",
        "맞고만살자",
        "거대망치",
        "빵셔틀",
        "킹망치"
    };

    private NetworkRunner runner;
    private readonly Dictionary<PlayerRef, NetworkObject> spawnedPlayers = new();
    private readonly List<NetworkObject> spawnedBots = new();
    private int nextJoinOrder = 1;
    private int pendingBotRespawns;
    private float nextBotCheckTime;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        if (!CanManageBots())
            return;

        TickBots(Time.deltaTime);

        if (Time.time < nextBotCheckTime)
            return;

        nextBotCheckTime = Time.time + botCheckInterval;
        EnsureBotPopulation();
    }

    public void Initialize(NetworkRunner networkRunner)
    {
        runner = networkRunner;
        RebuildSpawnedPlayers(networkRunner);
    }

    public void RebuildSpawnedPlayers(NetworkRunner networkRunner)
    {
        runner = networkRunner;
        spawnedPlayers.Clear();
        spawnedBots.Clear();

        NetworkPlayerStats[] players =
            FindObjectsByType<NetworkPlayerStats>(FindObjectsSortMode.None);

        foreach (NetworkPlayerStats player in players)
        {
            if (player == null || player.Object == null || !player.Object.IsValid)
                continue;

            if (player.IsBot)
            {
                spawnedBots.Add(player.Object);
                EnsureBotController(player.Object);
                continue;
            }

            PlayerRef inputAuthority = player.Object.InputAuthority;

            if (inputAuthority != PlayerRef.None)
                spawnedPlayers[inputAuthority] = player.Object;
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

    public void HandleCharacterFall(
        NetworkObject victimObject,
        KnockbackReceiver knockbackReceiver
    )
    {
        if (runner == null || !runner.IsServer)
            return;

        if (victimObject == null || !victimObject.IsValid)
            return;

        AwardKill(victimObject, knockbackReceiver);
        knockbackReceiver?.ClearLastAttacker();

        NetworkPlayerStats victimStats =
            victimObject.GetComponent<NetworkPlayerStats>();

        if (victimStats != null && victimStats.IsBot)
        {
            DespawnBot(victimObject);
            return;
        }

        if (victimObject.InputAuthority != PlayerRef.None)
            DespawnPlayer(victimObject.InputAuthority);
        else
            runner.Despawn(victimObject);
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
            GetSpawnPosition(),
            Quaternion.identity,
            player
        );

        spawnedPlayers[player] = playerObject;
        ConfigureCharacter(playerObject, nickname, character, connectionId, false);
    }

    private void EnsureBotPopulation()
    {
        CleanupBotList();

        int desiredBotCount = GetDesiredBotCount();
        int expectedBotCount = spawnedBots.Count + pendingBotRespawns;

        while (expectedBotCount < desiredBotCount)
        {
            if (!SpawnBot())
                break;

            expectedBotCount++;
        }
    }

    private bool SpawnBot()
    {
        if (playerPrefab == null)
            return false;

        CharacterData character = GetRandomCharacter();

        if (character == null)
            return false;

        NetworkObject botObject = runner.Spawn(
            playerPrefab,
            GetSpawnPosition(),
            Quaternion.identity,
            PlayerRef.None
        );

        if (botObject == null)
            return false;

        spawnedBots.Add(botObject);
        ConfigureCharacter(botObject, GetBotName(), character, "", true);
        EnsureBotController(botObject);
        return true;
    }

    private void ConfigureCharacter(
        NetworkObject playerObject,
        string nickname,
        CharacterData character,
        string connectionId,
        bool isBot
    )
    {
        if (playerObject == null)
            return;

        NetworkPlayerName playerName =
            playerObject.GetComponent<NetworkPlayerName>();

        if (playerName != null)
            playerName.SetNickname(nickname);

        NetworkPlayerStats stats =
            playerObject.GetComponent<NetworkPlayerStats>();

        if (stats != null)
        {
            stats.SetBot(isBot);
            stats.SetConnectionId(connectionId);
            stats.Apply(character);
        }
    }

    private void DespawnBot(NetworkObject botObject)
    {
        spawnedBots.Remove(botObject);

        if (botObject != null && botObject.IsValid)
            runner.Despawn(botObject);

        StartCoroutine(BotRespawnRoutine());
    }

    private IEnumerator BotRespawnRoutine()
    {
        pendingBotRespawns++;
        yield return new WaitForSeconds(botRespawnDelay);
        pendingBotRespawns = Mathf.Max(0, pendingBotRespawns - 1);
        EnsureBotPopulation();
    }

    private void AwardKill(
        NetworkObject victimObject,
        KnockbackReceiver knockbackReceiver
    )
    {
        if (knockbackReceiver == null)
            return;

        NetworkObject attackerObject = knockbackReceiver.LastAttackerObject;

        if (!IsValidAttacker(attackerObject, victimObject))
            attackerObject = FindPlayerObject(knockbackReceiver.LastAttacker);

        if (!IsValidAttacker(attackerObject, victimObject))
            return;

        NetworkPlayerStats attackerStats =
            attackerObject.GetComponent<NetworkPlayerStats>();

        NetworkPlayerStats victimStats =
            victimObject.GetComponent<NetworkPlayerStats>();

        bool attackerIsBot = attackerStats != null && attackerStats.IsBot;
        bool victimIsBot = victimStats != null && victimStats.IsBot;

        if (attackerIsBot && victimIsBot)
            return;

        NetworkPlayerScore attackerScore =
            attackerObject.GetComponent<NetworkPlayerScore>();

        if (attackerScore != null)
            attackerScore.AddKill();

        if (attackerStats != null)
            attackerStats.ApplyRandomKillReward();
    }

    private NetworkObject FindPlayerObject(PlayerRef player)
    {
        if (player == PlayerRef.None)
            return null;

        if (spawnedPlayers.TryGetValue(player, out NetworkObject playerObject) &&
            playerObject != null &&
            playerObject.IsValid)
        {
            return playerObject;
        }

        NetworkPlayerStats[] players =
            FindObjectsByType<NetworkPlayerStats>(FindObjectsSortMode.None);

        foreach (NetworkPlayerStats playerStats in players)
        {
            if (playerStats == null || playerStats.Object == null)
                continue;

            if (playerStats.Object.InputAuthority == player)
                return playerStats.Object;
        }

        return null;
    }

    private bool IsValidAttacker(
        NetworkObject attackerObject,
        NetworkObject victimObject
    )
    {
        return attackerObject != null &&
               attackerObject.IsValid &&
               attackerObject != victimObject;
    }

    private void TickBots(float deltaTime)
    {
        CleanupBotList();

        foreach (NetworkObject botObject in spawnedBots)
        {
            NetworkBotAI botAI = EnsureBotController(botObject);
            botAI?.ServerTick(deltaTime);
        }
    }

    private NetworkBotAI EnsureBotController(NetworkObject botObject)
    {
        if (botObject == null || !botObject.IsValid)
            return null;

        NetworkPlayerStats stats = botObject.GetComponent<NetworkPlayerStats>();

        if (stats == null || !stats.IsBot)
            return null;

        if (!botObject.TryGetComponent(out NetworkBotAI botAI))
            botAI = botObject.gameObject.AddComponent<NetworkBotAI>();

        return botAI;
    }

    private void CleanupBotList()
    {
        for (int i = spawnedBots.Count - 1; i >= 0; i--)
        {
            NetworkObject botObject = spawnedBots[i];

            if (botObject == null || !botObject.IsValid)
                spawnedBots.RemoveAt(i);
        }
    }

    private int GetDesiredBotCount()
    {
        if (targetCharacterCount <= 0)
            return 0;

        int humanCount = CountConnectedHumanPlayers();

        if (humanCount <= 0)
            return 0;

        int botsNeededForTargetCount = Mathf.Max(0, targetCharacterCount - humanCount);
        return Mathf.Max(botsNeededForTargetCount, minimumBotCount);
    }

    private int CountConnectedHumanPlayers()
    {
        if (runner == null)
            return spawnedPlayers.Count;

        int count = 0;

        foreach (PlayerRef _ in runner.CommittedPlayers)
        {
            count++;
        }

        return Mathf.Max(count, spawnedPlayers.Count);
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

    private CharacterData GetRandomCharacter()
    {
        if (characters == null || characters.Length == 0)
            return null;

        for (int i = 0; i < 12; i++)
        {
            CharacterData character = characters[Random.Range(0, characters.Length)];

            if (character != null)
                return character;
        }

        return null;
    }

    private string GetBotName()
    {
        if (botNames == null || botNames.Length == 0)
            return "망치봇";

        for (int i = 0; i < 12; i++)
        {
            string nickname = botNames[Random.Range(0, botNames.Length)];

            if (!IsNicknameInUse(nickname))
                return nickname;
        }

        return $"{botNames[Random.Range(0, botNames.Length)]}{Random.Range(10, 99)}";
    }

    private bool IsNicknameInUse(string nickname)
    {
        NetworkPlayerName[] players =
            FindObjectsByType<NetworkPlayerName>(FindObjectsSortMode.None);

        foreach (NetworkPlayerName player in players)
        {
            if (player == null)
                continue;

            if (player.Nickname.ToString() == nickname)
                return true;
        }

        return false;
    }

    private Vector3 GetSpawnPosition()
    {
        Vector2 randomPoint = Random.insideUnitCircle * botSpawnRadius;
        return new Vector3(randomPoint.x, spawnHeight, randomPoint.y);
    }

    private bool CanManageBots()
    {
        return runner != null &&
               runner.IsRunning &&
               runner.IsServer;
    }
}
