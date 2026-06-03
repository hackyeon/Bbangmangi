using Fusion;
using UnityEngine;

public class NetworkPlayerStats : NetworkBehaviour
{
    [Networked]
    public CharacterType CharacterType { get; set; }

    public void Apply(CharacterType characterType)
    {
        if (!HasStateAuthority)
            return;

        CharacterType = characterType;

        NetworkPlayerMotor motor = GetComponent<NetworkPlayerMotor>();
        BatAttack batAttack = GetComponent<BatAttack>();

        switch (characterType)
        {
            case CharacterType.Speed:
                if (motor != null)
                    motor.moveSpeed = 8f;

                if (batAttack != null)
                    batAttack.knockbackPower = 20f;
                break;

            case CharacterType.Balance:
                if (motor != null)
                    motor.moveSpeed = 6f;

                if (batAttack != null)
                    batAttack.knockbackPower = 26f;
                break;

            case CharacterType.Power:
                if (motor != null)
                    motor.moveSpeed = 4.5f;

                if (batAttack != null)
                    batAttack.knockbackPower = 34f;
                break;
        }
    }
}