using UnityEngine;
using UnityEngine.EventSystems;

public class AttackJoystick : MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler
{
    public RectTransform buttonTransform;
    public float pressedScale = 0.88f;
    public float animationSpeed = 18f;

    private Vector3 defaultScale;
    private Vector3 targetScale;

    private bool attackPressed;

    private void Start()
    {
        if (buttonTransform == null)
            buttonTransform = GetComponent<RectTransform>();

        defaultScale = buttonTransform.localScale;
        targetScale = defaultScale;
    }

    private void Update()
    {
        if (buttonTransform == null)
            return;

        buttonTransform.localScale = Vector3.Lerp(
            buttonTransform.localScale,
            targetScale,
            animationSpeed * Time.deltaTime
        );
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        attackPressed = true;
        targetScale = defaultScale * pressedScale;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        targetScale = defaultScale;
    }

    public bool ConsumeAttack()
    {
        if (!attackPressed)
            return false;

        attackPressed = false;
        return true;
    }
}