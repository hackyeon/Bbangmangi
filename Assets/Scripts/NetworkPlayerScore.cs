using Fusion;

public class NetworkPlayerScore : NetworkBehaviour
{
    public int KillCount { get; private set; }

    public void AddKill()
    {
        if (!HasStateAuthority)
            return;

        KillCount++;
    }
}