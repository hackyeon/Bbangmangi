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
    private readonly HashSet<string> reservedBotNames = new();
    private int nextJoinOrder = 1;
    private int pendingBotRespawns;
    private int botPopulationGeneration;
    private float nextBotCheckTime;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        if (!CanManageNetworkState())
            return;

        if (!NetworkRoundManager.IsGameplayActive)
        {
            if (spawnedBots.Count > 0)
                DespawnAllBots();

            return;
        }

        TickBots(Time.deltaTime);

        if (Time.time < nextBotCheckTime)
            return;

        nextBotCheckTime = Time.time + botCheckInterval;
        EnsureBotPopulation();
    }

    public void Initialize(NetworkRunner networkRunner)
    {
        runner = networkRunner;
        botPopulationGeneration++;
        pendingBotRespawns = 0;
        RebuildSpawnedPlayers(networkRunner);
    }

    public void RebuildSpawnedPlayers(NetworkRunner networkRunner)
    {
        runner = networkRunner;
        spawnedPlayers.Clear();
        spawnedBots.Clear();
        reservedBotNames.Clear();

        NetworkPlayerStats[] players =
            FindObjectsByType<NetworkPlayerStats>(FindObjectsSortMode.None);

        foreach (NetworkPlayerStats player in players)
        {
            if (player == null || player.Object == null || !player.Object.IsValid)
                continue;

            if (player.IsBot)
            {
                spawnedBots.Add(player.Object);
                ReserveExistingBotName(player.Object);
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

    public void HandleRoundPhaseStarted(RoundPhase phase)
    {
        if (!CanManageNetworkState())
            return;

        switch (phase)
        {
            case RoundPhase.CharacterSelect:
                BeginCharacterSelectionRound();
                break;

            case RoundPhase.Playing:
                BeginPlayingRound();
                break;

            case RoundPhase.Result:
                DespawnAllBots();
                break;
        }
    }

    public void HandleCharacterSelection(
        NetworkPlayerCommand playerCommand,
        string nickname,
        int characterId,
        string connectionId
    )
    {
        if (!CanManageNetworkState() ||
            playerCommand == null ||
            playerCommand.Object == null ||
            !playerCommand.Object.IsValid)
        {
            return;
        }

        NetworkRoundManager roundManager = NetworkRoundManager.Instance;

        if (roundManager == null ||
            (roundManager.Phase != RoundPhase.CharacterSelect &&
             roundManager.Phase != RoundPhase.Playing))
        {
            return;
        }

        CharacterData character = FindCharacter(characterId);

        nickname = nickname != null ? nickname.Trim() : "";

        if (character == null || string.IsNullOrEmpty(nickname))
            return;

        PlayerRef player = playerCommand.Object.InputAuthority;

        if (roundManager.Phase == RoundPhase.Playing &&
            FindPlayerObject(player) != null)
        {
            return;
        }

        if (IsNicknameUsedByAnotherPlayer(nickname, playerCommand))
        {
            playerCommand.RPC_RejectCharacterSelection(
                "이미 사용 중인 이름입니다."
            );
            return;
        }

        playerCommand.SetCharacterSelection(
            character.id,
            nickname,
            connectionId
        );

        if (roundManager.Phase == RoundPhase.Playing)
        {
            SpawnPlayer(
                player,
                nickname,
                character,
                playerCommand.ConnectionId.ToString()
            );
        }
    }

    public void HandleCharacterFall(
        NetworkObject victimObject,
        KnockbackReceiver knockbackReceiver
    )
    {
        if (runner == null || !runner.IsServer)
            return;

        if (!NetworkRoundManager.IsGameplayActive)
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

        NetworkObject playerObject = FindPlayerObject(player);

        if (playerObject != null && playerObject.IsValid)
            runner.Despawn(playerObject);

        spawnedPlayers.Remove(player);
    }

    private void SpawnPlayer(
        PlayerRef player,
        string nickname,
        CharacterData character,
        string connectionId
    )
    {
        if (!NetworkRoundManager.IsGameplayActive ||
            player == PlayerRef.None)
        {
            return;
        }

        if (spawnedPlayers.TryGetValue(player, out NetworkObject existingPlayer))
        {
            if (existingPlayer != null && existingPlayer.IsValid)
                return;

            spawnedPlayers.Remove(player);
        }

        NetworkObject restoredPlayer = FindPlayerObject(player);

        if (restoredPlayer != null && restoredPlayer.IsValid)
        {
            spawnedPlayers[player] = restoredPlayer;
            return;
        }

        NetworkObject playerObject = runner.Spawn(
            playerPrefab,
            GetSpawnPosition(),
            Quaternion.identity,
            player
        );

        spawnedPlayers[player] = playerObject;
        ConfigureCharacter(playerObject, nickname, character, connectionId, false);
    }

    private void BeginCharacterSelectionRound()
    {
        DespawnAllPlayers();
        DespawnAllBots();

        NetworkPlayerCommand[] playerCommands =
            FindObjectsByType<NetworkPlayerCommand>(FindObjectsSortMode.None);

        foreach (NetworkPlayerCommand playerCommand in playerCommands)
        {
            if (playerCommand == null || playerCommand.Object == null)
                continue;

            playerCommand.ResetCharacterSelection();
        }
    }

    private void BeginPlayingRound()
    {
        NetworkPlayerCommand[] playerCommands =
            FindObjectsByType<NetworkPlayerCommand>(FindObjectsSortMode.None);

        foreach (NetworkPlayerCommand playerCommand in playerCommands)
        {
            if (playerCommand == null ||
                playerCommand.Object == null ||
                !playerCommand.Object.IsValid)
            {
                continue;
            }

            PlayerRef player = playerCommand.Object.InputAuthority;

            if (player == PlayerRef.None || !IsConnectedPlayer(player))
                continue;

            CharacterData character = playerCommand.HasSelectedCharacter
                ? FindCharacter(playerCommand.SelectedCharacterId)
                : null;

            if (character == null)
            {
                character = GetDefaultCharacter();

                if (character == null)
                {
                    Debug.LogError("자동 배정할 유효한 캐릭터가 없습니다.");
                    continue;
                }

                playerCommand.AssignAutomaticCharacter(
                    character.id,
                    GetDefaultPlayerNickname(player)
                );
            }

            string nickname = playerCommand.SelectedNickname.ToString();

            if (string.IsNullOrEmpty(nickname))
                nickname = GetDefaultPlayerNickname(player);

            SpawnPlayer(
                player,
                nickname,
                character,
                playerCommand.ConnectionId.ToString()
            );
        }

        nextBotCheckTime = 0f;
        EnsureBotPopulation();
    }

    private void DespawnAllPlayers()
    {
        HashSet<NetworkObject> playerObjects = new();

        foreach (NetworkObject playerObject in spawnedPlayers.Values)
        {
            if (playerObject != null && playerObject.IsValid)
                playerObjects.Add(playerObject);
        }

        NetworkPlayerStats[] playerStats =
            FindObjectsByType<NetworkPlayerStats>(FindObjectsSortMode.None);

        foreach (NetworkPlayerStats stats in playerStats)
        {
            if (stats == null ||
                stats.Object == null ||
                !stats.Object.IsValid ||
                stats.IsBot)
            {
                continue;
            }

            playerObjects.Add(stats.Object);
        }

        foreach (NetworkObject playerObject in playerObjects)
            runner.Despawn(playerObject);

        spawnedPlayers.Clear();
    }

    private void DespawnAllBots()
    {
        botPopulationGeneration++;
        pendingBotRespawns = 0;
        CleanupBotList();

        foreach (NetworkObject botObject in spawnedBots)
        {
            ReleaseBotName(botObject);

            if (botObject != null && botObject.IsValid)
                runner.Despawn(botObject);
        }

        spawnedBots.Clear();
        reservedBotNames.Clear();
    }

    private void EnsureBotPopulation()
    {
        if (!CanManageBots())
            return;

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

        string botName = GetBotName();

        NetworkObject botObject = runner.Spawn(
            playerPrefab,
            GetSpawnPosition(),
            Quaternion.identity,
            PlayerRef.None
        );

        if (botObject == null)
        {
            reservedBotNames.Remove(botName);
            return false;
        }

        spawnedBots.Add(botObject);
        ConfigureCharacter(botObject, botName, character, "", true);
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
        ReleaseBotName(botObject);

        if (botObject != null && botObject.IsValid)
            runner.Despawn(botObject);

        if (NetworkRoundManager.IsGameplayActive)
            StartCoroutine(BotRespawnRoutine(botPopulationGeneration));
    }

    private IEnumerator BotRespawnRoutine(int generation)
    {
        pendingBotRespawns++;
        yield return new WaitForSeconds(botRespawnDelay);

        if (generation != botPopulationGeneration)
            yield break;

        pendingBotRespawns = Mathf.Max(0, pendingBotRespawns - 1);
        EnsureBotPopulation();
    }

    private void AwardKill(
        NetworkObject victimObject,
        KnockbackReceiver knockbackReceiver
    )
    {
        if (!NetworkRoundManager.IsGameplayActive ||
            knockbackReceiver == null)
        {
            return;
        }

        NetworkObject attackerObject = knockbackReceiver.LastAttackerObject;

        if (!IsValidAttacker(attackerObject, victimObject))
            attackerObject = FindPlayerObject(knockbackReceiver.LastAttacker);

        if (!IsValidAttacker(attackerObject, victimObject))
            return;

        NetworkPlayerStats attackerStats =
            attackerObject.GetComponent<NetworkPlayerStats>();

        NetworkPlayerStats victimStats =
            victimObject.GetComponent<NetworkPlayerStats>();

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
            {
                ReleaseBotName(botObject);
                spawnedBots.RemoveAt(i);
            }
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

    private CharacterData GetDefaultCharacter()
    {
        if (characters == null)
            return null;

        foreach (CharacterData character in characters)
        {
            if (character != null)
                return character;
        }

        return null;
    }

    private bool IsConnectedPlayer(PlayerRef player)
    {
        if (runner == null)
            return false;

        foreach (PlayerRef connectedPlayer in runner.CommittedPlayers)
        {
            if (connectedPlayer == player)
                return true;
        }

        return false;
    }

    private static string GetDefaultPlayerNickname(PlayerRef player)
    {
        return $"Player {player.PlayerId}";
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
        const string fallbackName = "망치봇";

        if (botNames != null && botNames.Length > 0)
        {
            int startIndex = Random.Range(0, botNames.Length);

            for (int i = 0; i < botNames.Length; i++)
            {
                string nickname = botNames[(startIndex + i) % botNames.Length];

                if (TryReserveBotName(nickname))
                    return nickname;
            }
        }

        string baseName = GetFallbackBotNameBase(fallbackName);

        for (int suffix = 2; suffix < 1000; suffix++)
        {
            string nickname = $"{baseName}{suffix}";

            if (TryReserveBotName(nickname))
                return nickname;
        }

        for (int suffix = 1000; suffix < 10000; suffix++)
        {
            string nickname = $"{fallbackName}{suffix}";

            if (TryReserveBotName(nickname))
                return nickname;
        }

        reservedBotNames.Add(fallbackName);
        return fallbackName;
    }

    private string GetFallbackBotNameBase(string fallbackName)
    {
        if (botNames == null)
            return fallbackName;

        foreach (string botName in botNames)
        {
            if (!string.IsNullOrWhiteSpace(botName))
                return botName.Trim();
        }

        return fallbackName;
    }

    private bool TryReserveBotName(string nickname)
    {
        if (string.IsNullOrWhiteSpace(nickname))
            return false;

        nickname = nickname.Trim();

        if (reservedBotNames.Contains(nickname) || IsNicknameInUse(nickname))
            return false;

        reservedBotNames.Add(nickname);
        return true;
    }

    private void ReserveExistingBotName(NetworkObject botObject)
    {
        NetworkPlayerName playerName = GetPlayerName(botObject);

        if (playerName == null)
            return;

        string nickname = playerName.Nickname.ToString();

        if (!string.IsNullOrWhiteSpace(nickname))
            reservedBotNames.Add(nickname.Trim());
    }

    private void ReleaseBotName(NetworkObject botObject)
    {
        NetworkPlayerName playerName = GetPlayerName(botObject);

        if (playerName == null)
            return;

        string nickname = playerName.Nickname.ToString();

        if (!string.IsNullOrWhiteSpace(nickname))
            reservedBotNames.Remove(nickname.Trim());
    }

    private static NetworkPlayerName GetPlayerName(NetworkObject playerObject)
    {
        return playerObject != null
            ? playerObject.GetComponent<NetworkPlayerName>()
            : null;
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

    private bool IsNicknameUsedByAnotherPlayer(
        string nickname,
        NetworkPlayerCommand requestingCommand
    )
    {
        NetworkPlayerCommand[] playerCommands =
            FindObjectsByType<NetworkPlayerCommand>(FindObjectsSortMode.None);

        foreach (NetworkPlayerCommand playerCommand in playerCommands)
        {
            if (playerCommand == null ||
                playerCommand == requestingCommand ||
                !playerCommand.HasSelectedCharacter)
            {
                continue;
            }

            if (string.Equals(
                    playerCommand.SelectedNickname.ToString(),
                    nickname,
                    System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        NetworkPlayerName[] playerNames =
            FindObjectsByType<NetworkPlayerName>(FindObjectsSortMode.None);

        foreach (NetworkPlayerName playerName in playerNames)
        {
            if (playerName == null || playerName.Object == null)
                continue;

            if (requestingCommand != null &&
                playerName.Object.InputAuthority ==
                requestingCommand.Object.InputAuthority)
            {
                continue;
            }

            if (string.Equals(
                    playerName.Nickname.ToString(),
                    nickname,
                    System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
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
        return CanManageNetworkState() &&
               NetworkRoundManager.IsGameplayActive;
    }

    private bool CanManageNetworkState()
    {
        return runner != null &&
               runner.IsRunning &&
               runner.IsServer;
    }
}
