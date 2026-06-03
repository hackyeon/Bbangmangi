using Fusion;

public class NetworkPlayerCommand : NetworkBehaviour
{
    public override void Spawned()
    {
        if (HasInputAuthority)
        {
            NetworkRunnerManager.Instance.SetLocalCommand(this);
        }
    }

    public void RequestSpawn(string nickname)
    {
        RPC_RequestSpawn(nickname);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_RequestSpawn(string nickname)
    {
        NetworkGameManager.Instance.RequestSpawn(
            Object.InputAuthority,
            nickname
        );
    }
}