using Fusion;
using UnityEngine;

public class NetworkPlayerStats : NetworkBehaviour
{
    [Networked]
    public int CharacterId { get; set; }

    public void Apply(CharacterData character)
    {
        if (!HasStateAuthority)
            return;

        CharacterId = character.id;

        NetworkPlayerMotor motor = GetComponent<NetworkPlayerMotor>();
        BatAttack batAttack = GetComponent<BatAttack>();

        if (motor != null)
            motor.moveSpeed = character.moveSpeed;

        if (batAttack != null)
            batAttack.knockbackPower = character.knockbackPower;
    }
}