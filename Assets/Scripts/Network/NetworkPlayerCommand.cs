using Fusion;
using UnityEngine;

public class NetworkPlayerCommand : NetworkBehaviour
{
    [Networked] public int HostScore { get; private set; }
    [Networked] public int JoinOrder { get; private set; }
    [Networked] public NetworkString<_32> ConnectionId { get; private set; }
    [Networked] public int SelectedCharacterId { get; private set; }
    [Networked] public NetworkBool HasSelectedCharacter { get; private set; }
    [Networked] public NetworkString<_16> SelectedNickname { get; private set; }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_SubmitHostCandidate(int score, string connectionId)
    {
        HostScore = Mathf.Clamp(score, 0, 10000);
        ConnectionId = connectionId;

        if (JoinOrder <= 0 && NetworkGameManager.Instance != null)
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

            if (NetworkRoundManager.Instance != null)
                NetworkRoundManager.Instance.RefreshLocalPresentation();
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

    public void SubmitCharacterSelection(string nickname, int characterId)
    {
        RPC_SubmitCharacterSelection(
            nickname,
            characterId,
            NetworkRunnerManager.LocalConnectionId
        );
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_SubmitCharacterSelection(
        string nickname,
        int characterId,
        string connectionId
    )
    {
        if (NetworkGameManager.Instance == null)
            return;

        NetworkGameManager.Instance.HandleCharacterSelection(
            this,
            nickname,
            characterId,
            connectionId
        );
    }

    public void SetCharacterSelection(
        int characterId,
        string nickname,
        string connectionId
    )
    {
        if (!HasStateAuthority)
            return;

        SelectedCharacterId = characterId;
        SelectedNickname = nickname;
        HasSelectedCharacter = true;

        if (!string.IsNullOrEmpty(connectionId))
            ConnectionId = connectionId;
    }

    public void AssignAutomaticCharacter(int characterId, string fallbackNickname)
    {
        if (!HasStateAuthority)
            return;

        SelectedCharacterId = characterId;
        HasSelectedCharacter = true;

        if (string.IsNullOrEmpty(SelectedNickname.ToString()))
            SelectedNickname = fallbackNickname;
    }

    public void ResetCharacterSelection()
    {
        if (!HasStateAuthority)
            return;

        SelectedCharacterId = -1;
        HasSelectedCharacter = false;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    public void RPC_RejectCharacterSelection(string message)
    {
        CharacterSelectUI characterSelectUI =
            FindFirstObjectByType<CharacterSelectUI>();

        characterSelectUI?.ShowSelectionError(message);
    }
}
