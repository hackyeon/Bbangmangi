using UnityEngine;

public class FallDetector : MonoBehaviour
{
    public Vector3 respawnPosition = new Vector3(0, 3, 0);
    public float fallY = -5f;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (transform.position.y < fallY)
        {
            Respawn();
        }
    }

    void Respawn()
    {
        transform.position = respawnPosition;

        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }
}