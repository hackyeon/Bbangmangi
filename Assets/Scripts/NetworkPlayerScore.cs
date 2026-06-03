using Fusion;

public class NetworkPlayerScore : NetworkBehaviour
{
    [Networked]
    public int KillCount { get; set; }

    public void AddKill()
    {
        if (!HasStateAuthority)
            return;

        KillCount++;
    }
}