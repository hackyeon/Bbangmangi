using Fusion;
using UnityEngine;

public class NetworkPlayerStats : NetworkBehaviour
{
    public Transform visualRoot;
    public RuntimeAnimatorController animatorController;
    public GameObject capsuleVisual;
    
    [Networked]
    public int CharacterId { get; set; }

    private int appliedCharacterId = -1;
    private GameObject currentModel;

    public override void Spawned()
    {
        ApplyVisualIfNeeded();
    }

    public override void Render()
    {
        ApplyVisualIfNeeded();
    }

    public void Apply(CharacterData character)
    {
        if (!HasStateAuthority)
            return;

        CharacterId = character.id;

        NetworkPlayerMotor motor = GetComponent<NetworkPlayerMotor>();
        BatAttack batAttack = GetComponent<BatAttack>();

        if (motor != null)
            motor.moveSpeed = character.moveSpeed;

        if (batAttack != null)
            batAttack.knockbackPower = character.knockbackPower;

        ApplyVisual(character);
    }

    private void ApplyVisualIfNeeded()
    {
        if (CharacterId == appliedCharacterId)
            return;

        CharacterData character = FindCharacter(CharacterId);

        if (character == null)
            return;

        ApplyVisual(character);
    }

    private CharacterData FindCharacter(int characterId)
    {
        if (NetworkGameManager.Instance == null)
            return null;

        foreach (CharacterData character in NetworkGameManager.Instance.characters)
        {
            if (character != null && character.id == characterId)
                return character;
        }

        return null;
    }

    private void ApplyVisual(CharacterData character)
    {
        if (visualRoot == null)
            visualRoot = transform.Find("Visual");

        if (capsuleVisual == null)
        {
            Transform capsule = transform.Find("CapsuleVisual");
            if (capsule != null)
                capsuleVisual = capsule.gameObject;
        }

        if (character.useCapsuleVisual)
        {
            if (visualRoot != null)
                visualRoot.gameObject.SetActive(false);

            if (capsuleVisual != null)
                capsuleVisual.SetActive(true);

            NetworkPlayerAnimation playerAnimation =
                GetComponent<NetworkPlayerAnimation>();

            if (playerAnimation != null)
            {
                playerAnimation.animator = null;
                playerAnimation.capsuleBatAttack =
                    GetComponentInChildren<CapsuleBatAttack>(true);
            }

            appliedCharacterId = character.id;
            return;
        }

        if (capsuleVisual != null)
            capsuleVisual.SetActive(false);

        if (visualRoot != null)
            visualRoot.gameObject.SetActive(true);

        if (visualRoot == null || character.modelPrefab == null)
            return;

        if (currentModel != null)
            Destroy(currentModel);

        foreach (Transform child in visualRoot)
        {
            Destroy(child.gameObject);
        }

        currentModel = Instantiate(
            character.modelPrefab,
            visualRoot
        );

        currentModel.transform.localPosition = Vector3.zero;
        currentModel.transform.localRotation = Quaternion.identity;
        currentModel.transform.localScale = Vector3.one;

        Animator animator = currentModel.GetComponentInChildren<Animator>();

        if (animator != null && animatorController != null)
            animator.runtimeAnimatorController = animatorController;

        NetworkPlayerAnimation animation =
            GetComponent<NetworkPlayerAnimation>();

        if (animation != null)
        {
            animation.animator = animator;
            animation.capsuleBatAttack = null;
        }

        appliedCharacterId = character.id;
    }
}