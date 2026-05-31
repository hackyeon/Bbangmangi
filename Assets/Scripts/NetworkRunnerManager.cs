using Fusion;
using UnityEngine;

public class NetworkRunnerManager : MonoBehaviour
{
    private NetworkRunner runner;

    async void Start()
    {
        runner = gameObject.AddComponent<NetworkRunner>();
        runner.ProvideInput = true;

        Debug.Log("[Bbangmangi] StartGame 시작");

        try
        {
            var result = await runner.StartGame(
                new StartGameArgs()
                {
                    GameMode = GameMode.AutoHostOrClient,
                    SessionName = "BbangmangiRoom",
                    SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
                });

            Debug.Log($"[Bbangmangi] Result OK = {result.Ok}");

            if (result.Ok)
            {
                Debug.Log("[Bbangmangi] 연결 성공");
            }
            else
            {
                Debug.LogError($"[Bbangmangi] 실패 : {result.ShutdownReason}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError(e);
        }
    }
}