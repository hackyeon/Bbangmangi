using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;

    public Vector3 offset = new Vector3(0, 12, -8);
    public float smoothSpeed = 12f;

    public Vector3 defaultPosition = new Vector3(0, 32, -8);

    void LateUpdate()
    {
        Vector3 desiredPosition;

        if (target != null)
        {
            desiredPosition = target.position + offset;
        }
        else
        {
            desiredPosition = defaultPosition;
        }

        transform.position = Vector3.Lerp(
            transform.position,
            desiredPosition,
            smoothSpeed * Time.deltaTime
        );
    }
}