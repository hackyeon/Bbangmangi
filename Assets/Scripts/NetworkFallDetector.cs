using Fusion;
using UnityEngine;

public class NetworkFallDetector : NetworkBehaviour
{
    public Vector3 respawnPosition = new Vector3(0, 5, 0);
    public float fallY = -5f;

    private Rigidbody rb;
    private KnockbackReceiver knockbackReceiver;

    public override void Spawned()
    {
        rb = GetComponent<Rigidbody>();
        knockbackReceiver = GetComponent<KnockbackReceiver>();
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority)
            return;

        if (transform.position.y < fallY)
        {
            GiveKillToLastAttacker();
            Respawn();
        }
    }

    private void GiveKillToLastAttacker()
    {
        if (knockbackReceiver == null)
            return;

        PlayerRef attacker = knockbackReceiver.LastAttacker;

        if (attacker == PlayerRef.None)
            return;

        NetworkPlayerScore[] players = FindObjectsOfType<NetworkPlayerScore>();

        foreach (NetworkPlayerScore player in players)
        {
            if (player.Object.InputAuthority == attacker)
            {
                player.AddKill();
                break;
            }
        }

        knockbackReceiver.ClearLastAttacker();
    }

    private void Respawn()
    {
        transform.position = respawnPosition;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }
}