using UnityEngine;

public class AutoDestroy : MonoBehaviour
{
    public float lifeTime = 1f;

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }
}