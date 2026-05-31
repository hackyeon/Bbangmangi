using UnityEngine;
using UnityEngine.EventSystems;

public class AttackJoystick : MonoBehaviour, IPointerDownHandler
{
    private bool hasAttack;

    public bool ConsumeAttack(out Vector2 direction)
    {
        direction = Vector2.zero;

        if (!hasAttack)
            return false;

        hasAttack = false;
        return true;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        hasAttack = true;
    }
}