using Fusion;
using UnityEngine;

public class NetworkFallDetector : NetworkBehaviour
{
    public float fallY = -10f;

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority)
            return;

        if (transform.position.y < fallY)
        {
            NetworkGameManager.Instance.DespawnPlayer(Object.InputAuthority);
        }
    }
}