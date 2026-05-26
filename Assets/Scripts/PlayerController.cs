using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;

    void Update()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move = new Vector3(x, 0, z);

        transform.position += move * (moveSpeed * Time.deltaTime);

        if (move != Vector3.zero)
        {
            transform.forward = move;
        }
    }
}