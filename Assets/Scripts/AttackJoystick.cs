using UnityEngine;
using UnityEngine.EventSystems;

public class AttackJoystick : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
{
    public RectTransform handle;
    public float handleRange = 60f;

    public Vector2 Direction { get; private set; }

    private RectTransform rectTransform;
    private PlayerAttackInput playerAttackInput;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();

        if (handle != null)
        {
            handle.gameObject.SetActive(true);
            handle.anchoredPosition = Vector2.zero;
        }

        playerAttackInput = FindObjectOfType<PlayerAttackInput>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 localPoint;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out localPoint
        );

        Vector2 clamped = Vector2.ClampMagnitude(localPoint, handleRange);

        handle.anchoredPosition = clamped;
        Direction = clamped / handleRange;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (Direction != Vector2.zero && playerAttackInput != null)
        {
            playerAttackInput.Attack(Direction);
        }

        handle.anchoredPosition = Vector2.zero;
        Direction = Vector2.zero;
    }
}