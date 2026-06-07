using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Fusion;
using Fusion.Sockets;
using UnityEngine;

public class NetworkRunnerManager : MonoBehaviour, INetworkRunnerCallbacks
{
    public static NetworkRunnerManager Instance { get; private set; }
    
    public NetworkObject playerPrefab;
    public MobileJoystick moveJoystick;
    public AttackJoystick attackJoystick;
    public NetworkObject commandPrefab;
    [SerializeField, Min(0.1f)] private float hostStallNoticeDelay = 0.7f;
    
    private NetworkPlayerCommand localCommand;
    private readonly Dictionary<PlayerRef, NetworkObject> playerCommands = new();
    private NetworkRunner runner;
    private CharacterSelectUI characterSelectUI;
    
    private const string SessionName = "BbangmangiRoom";
    private const string HostMigrationMessage =
        "호스트를 전환하는 중입니다...\n잠시만 기다려 주세요.";
    private const string HostMigrationFailedMessage =
        "호스트 전환에 실패했습니다.\n다시 접속해 주세요.";

    public static readonly string LocalConnectionId =
        Guid.NewGuid().ToString("N");

    public bool IsHostMigrating { get; private set; }

    private Tick lastObservedServerTick;
    private float lastServerTickAdvanceTime;
    private bool hasObservedServerTick;
    private bool isShowingHostStallNotice;

    private void Awake()
    {
        Instance = this;
    }
    
    public void SetLocalCommand(NetworkPlayerCommand command)
    {
        localCommand = command;
        ResetHostStallMonitor(false);
    }
    
    async void Start()
    {
        characterSelectUI = FindFirstObjectByType<CharacterSelectUI>();

        runner = CreateRunner();
        ResetHostStallMonitor(false);

        if (NetworkGameManager.Instance != null)
            NetworkGameManager.Instance.Initialize(runner);

        var result = await runner.StartGame(
            new StartGameArgs()
            {
                GameMode = GameMode.AutoHostOrClient,
                SessionName = SessionName,
                ConnectionToken = GetLocalConnectionToken(),
                SceneManager = runner.GetComponent<NetworkSceneManagerDefault>()
            });

        if (result.Ok)
        {
            if (characterSelectUI != null)
                characterSelectUI.SetStartButtonEnabled(true);
        }
        else
        {
            Debug.LogError($"Fusion 연결 실패: {result.ShutdownReason}");
        }
    }

    private void Update()
    {
        UpdateHostStallNotice();
    }
    
    private void RebuildPlayerCommands()
    {
        playerCommands.Clear();

        NetworkPlayerCommand[] commands =
            FindObjectsByType<NetworkPlayerCommand>(FindObjectsSortMode.None);

        foreach (NetworkPlayerCommand command in commands)
        {
            if (command == null || command.Object == null)
                continue;

            playerCommands[command.Object.InputAuthority] = command.Object;

            if (command.HasInputAuthority)
                localCommand = command;
        }
    }

    public void RequestSpawn(string nickname, int characterId)
    {
        if (localCommand == null)
        {
            Debug.LogError("Local PlayerCommand is not ready");
            return;
        }

        localCommand.RequestSpawn(nickname, characterId);
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (!runner.IsServer)
            return;

        if (playerCommands.ContainsKey(player))
            return;

        NetworkObject commandObject = runner.Spawn(
            commandPrefab,
            Vector3.zero,
            Quaternion.identity,
            player
        );

        playerCommands[player] = commandObject;
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (NetworkGameManager.Instance != null)
            NetworkGameManager.Instance.DespawnPlayer(player);

        if (!runner.IsServer)
            return;

        if (playerCommands.TryGetValue(player, out NetworkObject commandObject))
        {
            if (commandObject != null)
                runner.Despawn(commandObject);

            playerCommands.Remove(player);
        }
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        BbangmangiInputData data = new BbangmangiInputData();

        Vector2 move = Vector2.zero;

        if (moveJoystick != null)
            move = moveJoystick.Direction;

#if UNITY_EDITOR
        if (move == Vector2.zero)
        {
            move.x = Input.GetAxis("Horizontal");
            move.y = Input.GetAxis("Vertical");
        }
#endif

        data.MoveDirection = move;

        if (attackJoystick != null && attackJoystick.ConsumeAttack())
            data.AttackPressed = true;

#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.Space))
            data.AttackPressed = true;
#endif
        
        input.Set(data);
    }
    
    private NetworkRunner CreateRunner()
    {
        GameObject runnerObject = new GameObject("FusionRunner");
        runnerObject.transform.SetParent(transform);

        NetworkRunner newRunner = runnerObject.AddComponent<NetworkRunner>();
        newRunner.ProvideInput = true;
        newRunner.AddCallbacks(this);
        runnerObject.AddComponent<NetworkSceneManagerDefault>();

        return newRunner;
    }
    
    private void HostMigrationResume(NetworkRunner migrationRunner)
    {
        foreach (NetworkObject resumeObject in migrationRunner.GetResumeSnapshotNetworkObjects())
        {
            PlayerRef inputAuthority = resumeObject.InputAuthority;

            Vector3 position = resumeObject.transform.position;
            Quaternion rotation = resumeObject.transform.rotation;

            if (resumeObject.TryGetBehaviour<NetworkTRSP>(out var trsp))
            {
                position = trsp.Data.Position;
                rotation = trsp.Data.Rotation;
            }

            migrationRunner.Spawn(
                resumeObject,
                position,
                rotation,
                inputAuthority,
                onBeforeSpawned: (_, newObject) =>
                {
                    newObject.CopyStateFrom(resumeObject);
                }
            );
        }

        NetworkGameManager.Instance.RebuildSpawnedPlayers(migrationRunner);
        RebuildPlayerCommands();
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public async void OnHostMigration(
        NetworkRunner oldRunner,
        HostMigrationToken hostMigrationToken
    )
    {
        IsHostMigrating = true;
        localCommand = null;
        playerCommands.Clear();
        ShowHostMigrationNotice(HostMigrationMessage);
        ResetHostStallMonitor(false);

        await oldRunner.Shutdown(shutdownReason: ShutdownReason.HostMigration);
        Destroy(oldRunner.gameObject);

        runner = CreateRunner();
        ResetHostStallMonitor(false);
        
        if (NetworkGameManager.Instance != null)
            NetworkGameManager.Instance.Initialize(runner);

        StartGameResult result = await runner.StartGame(new StartGameArgs()
        {
            HostMigrationToken = hostMigrationToken,
            HostMigrationResume = HostMigrationResume,
            ConnectionToken = GetLocalConnectionToken(),
            SceneManager = runner.GetComponent<NetworkSceneManagerDefault>()
        });

        if (!result.Ok)
        {
            Debug.LogWarning($"Host Migration failed: {result.ShutdownReason}");
            IsHostMigrating = false;
            ShowHostMigrationNotice(HostMigrationFailedMessage);
            return;
        }

        IsHostMigrating = false;

        await runner.PushHostMigrationSnapshot();
        StartCoroutine(CleanupDisconnectedPlayersAfterMigration(runner));
        StartCoroutine(HideHostMigrationNoticeAfterDelay(3.2f));
    }

    private void ShowHostMigrationNotice(string message)
    {
        HostMigrationNoticeUI.GetOrCreate().Show(message);
    }

    private IEnumerator HideHostMigrationNoticeAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        HostMigrationNoticeUI.GetOrCreate().Hide();
    }

    private void UpdateHostStallNotice()
    {
        if (IsHostMigrating)
            return;

        // Fusion confirms host migration after a timeout, so show feedback as soon as server ticks stall.
        if (!ShouldWatchForHostStall())
        {
            ResetHostStallMonitor(true);
            return;
        }

        Tick currentServerTick = runner.LatestServerTick;

        if (!hasObservedServerTick ||
            !currentServerTick.Equals(lastObservedServerTick))
        {
            lastObservedServerTick = currentServerTick;
            lastServerTickAdvanceTime = Time.unscaledTime;
            hasObservedServerTick = true;

            if (isShowingHostStallNotice)
            {
                isShowingHostStallNotice = false;
                HostMigrationNoticeUI.GetOrCreate().Hide();
            }

            return;
        }

        if (isShowingHostStallNotice)
            return;

        if (Time.unscaledTime - lastServerTickAdvanceTime < hostStallNoticeDelay)
            return;

        isShowingHostStallNotice = true;
        ShowHostMigrationNotice(HostMigrationMessage);
    }

    private bool ShouldWatchForHostStall()
    {
        return runner != null &&
               runner.IsRunning &&
               runner.IsClient &&
               !runner.IsServer &&
               localCommand != null;
    }

    private void ResetHostStallMonitor(bool hideStallNotice)
    {
        if (hideStallNotice && isShowingHostStallNotice && !IsHostMigrating)
            HostMigrationNoticeUI.GetOrCreate().Hide();

        hasObservedServerTick = false;
        lastServerTickAdvanceTime = Time.unscaledTime;
        isShowingHostStallNotice = false;
    }

    private IEnumerator CleanupDisconnectedPlayersAfterMigration(NetworkRunner migrationRunner)
    {
        yield return new WaitForSecondsRealtime(3f);

        if (migrationRunner == null || !migrationRunner.IsRunning || !migrationRunner.IsServer)
            yield break;

        HashSet<PlayerRef> activePlayers = new();
        HashSet<string> activeConnectionIds = new();

        foreach (PlayerRef player in migrationRunner.CommittedPlayers)
        {
            activePlayers.Add(player);
            string connectionId =
                GetConnectionId(migrationRunner.GetPlayerConnectionToken(player));

            if (!string.IsNullOrEmpty(connectionId))
                activeConnectionIds.Add(connectionId);
        }

        activeConnectionIds.Add(LocalConnectionId);

        IsHostMigrating = true;

        NetworkPlayerStats[] players =
            FindObjectsByType<NetworkPlayerStats>(FindObjectsSortMode.None);

        foreach (NetworkPlayerStats player in players)
        {
            if (player == null || player.Object == null)
                continue;

            PlayerRef inputAuthority = player.Object.InputAuthority;
            string connectionId = player.ConnectionId.ToString();

            if (IsConnectedPlayer(
                    connectionId,
                    inputAuthority,
                    activeConnectionIds,
                    activePlayers))
                continue;

            migrationRunner.Despawn(player.Object);
        }

        NetworkPlayerCommand[] commands =
            FindObjectsByType<NetworkPlayerCommand>(FindObjectsSortMode.None);

        foreach (NetworkPlayerCommand command in commands)
        {
            if (command == null || command.Object == null)
                continue;

            PlayerRef inputAuthority = command.Object.InputAuthority;
            string connectionId = command.ConnectionId.ToString();

            if (IsConnectedPlayer(
                    connectionId,
                    inputAuthority,
                    activeConnectionIds,
                    activePlayers))
                continue;

            migrationRunner.Despawn(command.Object);
            playerCommands.Remove(inputAuthority);
        }

        IsHostMigrating = false;
        NetworkGameManager.Instance.RebuildSpawnedPlayers(migrationRunner);
        RebuildPlayerCommands();
    }

    private static byte[] GetLocalConnectionToken()
    {
        return Encoding.UTF8.GetBytes(LocalConnectionId);
    }

    private static string GetConnectionId(byte[] connectionToken)
    {
        if (connectionToken == null || connectionToken.Length == 0)
            return "";

        return Encoding.UTF8.GetString(connectionToken);
    }

    private static bool IsConnectedPlayer(
        string connectionId,
        PlayerRef inputAuthority,
        HashSet<string> activeConnectionIds,
        HashSet<PlayerRef> activePlayers
    )
    {
        if (!string.IsNullOrEmpty(connectionId))
            return activeConnectionIds.Contains(connectionId);

        return inputAuthority != PlayerRef.None &&
               activePlayers.Contains(inputAuthority);
    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, System.ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
}
