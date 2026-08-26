using Fusion;

public class NetworkPlayerScore : NetworkBehaviour
{
    private const int BaseKillScore = 1;
    private const int KingBountyBonus = 1;

    [Networked] public int KillCount { get; private set; }
    [Networked] public int RoundScore { get; private set; }
    [Networked] public NetworkBool IsKing { get; private set; }

    public bool RegisterKill(bool defeatedKing)
    {
        if (!HasStateAuthority || !NetworkRoundManager.IsGameplayActive)
            return false;

        KillCount++;
        RoundScore += BaseKillScore + (defeatedKing ? KingBountyBonus : 0);
        return true;
    }

    public void SetKingState(bool isKing)
    {
        if (!HasStateAuthority)
            return;

        IsKing = isKing;
    }

    public void ResetRoundData()
    {
        if (!HasStateAuthority)
            return;

        KillCount = 0;
        RoundScore = 0;
        IsKing = false;
    }
}
