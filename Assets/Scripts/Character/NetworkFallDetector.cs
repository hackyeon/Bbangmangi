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
            GiveKillToLastAttacker();
            NetworkGameManager.Instance.DespawnPlayer(Object.InputAuthority);
        }
    }

    private void GiveKillToLastAttacker()
    {
        if (knockbackReceiver == null)
            return;

        PlayerRef attacker = knockbackReceiver.LastAttacker;

        if (attacker == PlayerRef.None)
            return;

        if (attacker == Object.InputAuthority)
            return;

        NetworkPlayerScore[] players =
            FindObjectsByType<NetworkPlayerScore>(
                FindObjectsSortMode.None
            );

        foreach (NetworkPlayerScore player in players)
        {
            if (player == null || player.Object == null)
                continue;

            if (player.Object.InputAuthority == attacker)
            {
                player.AddKill();
                break;
            }
        }

        knockbackReceiver.ClearLastAttacker();
    }
}