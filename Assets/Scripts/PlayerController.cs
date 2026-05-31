using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public MobileJoystick joystick;

    private Rigidbody rb;
    private KnockbackReceiver knockbackReceiver;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        knockbackReceiver = GetComponent<KnockbackReceiver>();
    }

    void FixedUpdate()
    {
        if (knockbackReceiver != null &&
            knockbackReceiver.IsStunned)
        {
            return;
        }
        
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        if (joystick != null && joystick.Direction != Vector2.zero)
        {
            x = joystick.Direction.x;
            z = joystick.Direction.y;
        }

        Vector3 move = new Vector3(x, 0, z);

        rb.linearVelocity = new Vector3(
            move.x * moveSpeed,
            rb.linearVelocity.y,
            move.z * moveSpeed
        );

        if (move != Vector3.zero)
        {
            transform.forward = move;
        }
    }
}