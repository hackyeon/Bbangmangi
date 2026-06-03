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

    public void RequestSpawn(string nickname, CharacterType characterType)
    {
        RPC_RequestSpawn(nickname, (int)characterType);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_RequestSpawn(string nickname, int characterType)
    {
        NetworkGameManager.Instance.RequestSpawn(
            Object.InputAuthority,
            nickname,
            (CharacterType)characterType
        );
    }
}