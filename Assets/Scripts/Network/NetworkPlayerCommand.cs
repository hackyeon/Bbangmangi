using Fusion;
using UnityEngine;

public class NetworkPlayerCommand : NetworkBehaviour
{
    [Networked] public int HostScore { get; private set; }
    [Networked] public int JoinOrder { get; private set; }
    [Networked] public NetworkString<_32> ConnectionId { get; private set; }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_SubmitHostCandidate(int score, string connectionId)
    {
        HostScore = Mathf.Clamp(score, 0, 10000);
        ConnectionId = connectionId;

        if (JoinOrder <= 0)
            JoinOrder = NetworkGameManager.Instance.NextJoinOrder();
    }
    
    public override void Spawned()
    {
        if (HasInputAuthority)
        {
            string connectionId = ConnectionId.ToString();

            if (!string.IsNullOrEmpty(connectionId) &&
                connectionId != NetworkRunnerManager.LocalConnectionId)
            {
                return;
            }

            NetworkRunnerManager.Instance.SetLocalCommand(this);
            RPC_SubmitHostCandidate(
                CalculateHostCandidateScore(),
                NetworkRunnerManager.LocalConnectionId
            );
        }
    }
    
    private int CalculateHostCandidateScore()
    {
        int score = 1000;

        if (Application.internetReachability == NetworkReachability.ReachableViaLocalAreaNetwork)
            score += 300;
        else if (Application.internetReachability == NetworkReachability.ReachableViaCarrierDataNetwork)
            score += 100;
        else
            score -= 500;

        score += Mathf.Min(SystemInfo.processorCount, 12) * 30;
        score += Mathf.Clamp(SystemInfo.systemMemorySize / 512, 0, 32) * 10;

        if (SystemInfo.batteryStatus == BatteryStatus.Charging ||
            SystemInfo.batteryStatus == BatteryStatus.Full)
            score += 150;
        else if (SystemInfo.batteryLevel > 0f && SystemInfo.batteryLevel < 0.25f)
            score -= 300;

#if UNITY_WEBGL
    score -= 500;
#endif

        return score;
    }

    public void RequestSpawn(string nickname, int characterId)
    {
        RPC_RequestSpawn(
            nickname,
            characterId,
            NetworkRunnerManager.LocalConnectionId
        );
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_RequestSpawn(
        string nickname,
        int characterId,
        string connectionId
    )
    {
        NetworkGameManager.Instance.RequestSpawn(
            Object.InputAuthority,
            nickname,
            characterId,
            connectionId
        );
    }
}
