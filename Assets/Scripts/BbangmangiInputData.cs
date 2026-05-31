using Fusion;
using UnityEngine;

public struct BbangmangiInputData : INetworkInput
{
    public Vector2 MoveDirection;
    public Vector2 AttackDirection;
    public NetworkBool AttackPressed;
}