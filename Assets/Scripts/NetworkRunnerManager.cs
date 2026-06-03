using System.Collections.Generic;
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
    
    private NetworkPlayerCommand localCommand;
    private readonly Dictionary<PlayerRef, NetworkObject> playerCommands = new();
    private NetworkRunner runner;
    private CharacterSelectUI characterSelectUI;

    private void Awake()
    {
        Instance = this;
    }
    
    public void SetLocalCommand(NetworkPlayerCommand command)
    {
        localCommand = command;
    }
    
    async void Start()
    {
        characterSelectUI = FindFirstObjectByType<CharacterSelectUI>();

        runner = gameObject.AddComponent<NetworkRunner>();
        runner.ProvideInput = true;
        runner.AddCallbacks(this);

        var result = await runner.StartGame(
            new StartGameArgs()
            {
                GameMode = GameMode.AutoHostOrClient,
                SessionName = "BbangmangiRoom",
                SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
            });

        if (result.Ok)
        {
            if (NetworkGameManager.Instance != null)
                NetworkGameManager.Instance.Initialize(runner);

            if (characterSelectUI != null)
                characterSelectUI.SetStartButtonEnabled(true);
        }
        else
        {
            Debug.LogError($"Fusion 연결 실패: {result.ShutdownReason}");
        }
    }

    public void RequestSpawn(string nickname)
    {
        if (localCommand == null)
        {
            Debug.LogError("Local PlayerCommand is not ready");
            return;
        }

        localCommand.RequestSpawn(nickname);
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

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, System.ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
}