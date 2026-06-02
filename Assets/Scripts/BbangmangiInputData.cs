using Fusion;
using UnityEngine;

public struct BbangmangiInputData : INetworkInput
{
    public Vector2 MoveDirection;
    public NetworkBool AttackPressed;
    public NetworkBool StartPressed;
}