using Fusion;
using UnityEngine;

public class NetworkPlayerCommand : NetworkBehaviour
{
    public override void FixedUpdateNetwork()
    {
        if (!GetInput(out BbangmangiInputData input))
            return;

        if (input.StartPressed && HasStateAuthority)
        {
            NetworkGameManager.Instance.RequestSpawn(Object.InputAuthority);
        }
    }
}