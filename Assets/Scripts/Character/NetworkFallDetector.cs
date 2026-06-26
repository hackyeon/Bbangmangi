using Fusion;
using UnityEngine;

public class NetworkFallDetector : NetworkBehaviour
{
    public float fallY = -10f;

    private KnockbackReceiver knockbackReceiver;

    public override void Spawned()
    {
        knockbackReceiver = GetComponent<KnockbackReceiver>();
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority)
            return;

        if (transform.position.y < fallY)
        {
            if (NetworkGameManager.Instance != null)
                NetworkGameManager.Instance.HandleCharacterFall(Object, knockbackReceiver);
        }
    }
}
