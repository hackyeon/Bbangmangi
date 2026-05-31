using Fusion;
using UnityEngine;

public class NetworkRunnerManager : MonoBehaviour
{
    public NetworkObject playerPrefab;

    private NetworkRunner runner;

    async void Start()
    {
        runner = gameObject.AddComponent<NetworkRunner>();
        runner.ProvideInput = true;

        Debug.Log("[Bbangmangi] StartGame 시작");

        var result = await runner.StartGame(
            new StartGameArgs()
            {
                GameMode = GameMode.AutoHostOrClient,
                SessionName = "BbangmangiRoom",
                SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
            });

        if (result.Ok)
        {
            Debug.Log("[Bbangmangi] 연결 성공");

            SpawnPlayer();
        }
        else
        {
            Debug.LogError($"[Bbangmangi] 연결 실패: {result.ShutdownReason}");
        }
    }

    private void SpawnPlayer()
    {
        Vector3 spawnPos = new Vector3(0, 5, 0);

        NetworkObject playerObject = runner.Spawn(
            playerPrefab,
            spawnPos,
            Quaternion.identity,
            runner.LocalPlayer
        );

        CameraFollow cameraFollow = Camera.main.GetComponent<CameraFollow>();
        if (cameraFollow != null)
        {
            cameraFollow.target = playerObject.transform;
        }
    }
}