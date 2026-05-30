using UnityEngine;

public class FallDetector : MonoBehaviour
{
    public Vector3 respawnPosition = new Vector3(0, 3, 0);
    public float fallY = -5f;

    public bool isPlayer;

    private Rigidbody rb;
    private GameManager gameManager;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        gameManager = FindObjectOfType<GameManager>();
    }

    void Update()
    {
        if (transform.position.y < fallY)
        {
            if (isPlayer)
            {
                gameManager.AddEnemyScore();
            }
            else
            {
                gameManager.AddPlayerScore();
            }

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