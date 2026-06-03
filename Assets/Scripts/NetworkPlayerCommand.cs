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

    public void RequestSpawn(string nickname, int characterId)
    {
        RPC_RequestSpawn(nickname, characterId);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_RequestSpawn(string nickname, int characterId)
    {
        NetworkGameManager.Instance.RequestSpawn(
            Object.InputAuthority,
            nickname,
            characterId
        );
    }
}