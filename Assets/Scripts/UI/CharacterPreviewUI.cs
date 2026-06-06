using UnityEngine;

public class CharacterPreviewUI : MonoBehaviour
{
    public RenderTexture previewTexture;
    public Transform previewRoot;
    public RuntimeAnimatorController animatorController;
    public float rotationSpeed = 35f;

    private GameObject currentModel;

    public Texture PreviewTexture => previewTexture;

    public void Show(CharacterData character)
    {
        Clear();

        if (character == null || character.modelPrefab == null)
            return;

        currentModel = Instantiate(
            character.modelPrefab,
            previewRoot
        );

        currentModel.transform.localPosition = character.previewPosition;
        currentModel.transform.localRotation =
            Quaternion.Euler(character.previewRotation);
        currentModel.transform.localScale = character.previewScale;

        DisablePhysics(currentModel);
        SetupAnimator(currentModel);
    }

    public void Clear()
    {
        if (currentModel != null)
        {
            Destroy(currentModel);
            currentModel = null;
        }
    }

    private void Update()
    {
        if (currentModel == null)
            return;

        currentModel.transform.Rotate(
            Vector3.up,
            rotationSpeed * Time.deltaTime,
            Space.World
        );
    }

    private void SetupAnimator(GameObject model)
    {
        Animator animator = model.GetComponentInChildren<Animator>();

        if (animator == null)
            return;

        if (animatorController != null)
            animator.runtimeAnimatorController = animatorController;

        animator.SetFloat("Speed", 0f);
    }

    private void DisablePhysics(GameObject model)
    {
        Collider[] colliders =
            model.GetComponentsInChildren<Collider>();

        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }

        Rigidbody[] rigidbodies =
            model.GetComponentsInChildren<Rigidbody>();

        foreach (Rigidbody rb in rigidbodies)
        {
            rb.isKinematic = true;
        }
    }
}