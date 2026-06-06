using UnityEngine;

public class CapsuleBatAttack : MonoBehaviour
{
    public Transform batTransform;

    public float swingAngle = 100f;
    public float swingDuration = 0.14f;
    public float returnDuration = 0.14f;

    private bool isSwinging;
    private float timer;
    private Quaternion defaultRotation;
    private Quaternion targetRotation;

    private void Awake()
    {
        if (batTransform != null)
        {
            defaultRotation = batTransform.localRotation;
            targetRotation =
                defaultRotation *
                Quaternion.Euler(-swingAngle, 0f, 0f);
        }
    }

    public void PlaySwing()
    {
        if (batTransform == null)
            return;

        isSwinging = true;
        timer = 0f;
    }

    private void Update()
    {
        if (!isSwinging || batTransform == null)
            return;

        timer += Time.deltaTime;

        if (timer <= swingDuration)
        {
            float t = timer / swingDuration;

            batTransform.localRotation =
                Quaternion.Slerp(defaultRotation, targetRotation, t);
        }
        else if (timer <= swingDuration + returnDuration)
        {
            float t = (timer - swingDuration) / returnDuration;

            batTransform.localRotation =
                Quaternion.Slerp(targetRotation, defaultRotation, t);
        }
        else
        {
            batTransform.localRotation = defaultRotation;
            isSwinging = false;
        }
    }
}