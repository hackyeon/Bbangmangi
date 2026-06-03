using UnityEngine;
using UnityEngine.EventSystems;

public class AttackJoystick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    private bool hasAttack;

    public bool ConsumeAttack()
    {
        if (!hasAttack)
            return false;

        hasAttack = false;
        return true;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        hasAttack = true;
        Debug.Log("Attack Button Down");
    }

    public void OnPointerUp(PointerEventData eventData)
    {
    }
}