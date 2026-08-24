using Fusion;
using UnityEngine;

public class NetworkPlayerStats : NetworkBehaviour
{
    private const float MoveSpeedReward = 2f;
    private const float MaxMoveSpeed = 20f;
    private const float KnockbackPowerReward = 5f;
    private const float MaxKnockbackPower = 100f;
    private const float AttackRangeReward = 0.25f;
    private const float MaxAttackRange = 4.2f;

    public Transform visualRoot;
    public RuntimeAnimatorController animatorController;
    public GameObject capsuleVisual;
    
    [Networked]
    public int CharacterId { get; set; }

    [Networked]
    public NetworkString<_32> ConnectionId { get; set; }

    [Networked]
    public NetworkBool IsBot { get; private set; }

    [Networked]
    public float CurrentMoveSpeed { get; private set; }

    [Networked]
    public float CurrentKnockbackPower { get; private set; }

    [Networked]
    public float CurrentAttackRange { get; private set; }

    private int appliedCharacterId = -1;
    private GameObject currentModel;
    private NetworkKillScaleVisual killScaleVisual;

    public override void Spawned()
    {
        ApplyVisualIfNeeded();
        ApplyRuntimeStats();
        ApplyKillScale();
    }

    public override void Render()
    {
        ApplyVisualIfNeeded();
        ApplyRuntimeStats();
        ApplyKillScale();
    }

    public void Apply(CharacterData character)
    {
        if (!HasStateAuthority)
            return;

        CharacterId = character.id;

        NetworkPlayerMotor motor = GetComponent<NetworkPlayerMotor>();
        BatAttack batAttack = GetComponent<BatAttack>();

        CurrentMoveSpeed = character.moveSpeed;
        CurrentKnockbackPower = character.knockbackPower;

        if (batAttack != null)
            CurrentAttackRange = batAttack.attackRange;

        ApplyRuntimeStats();
        ApplyVisual(character);
    }

    public void SetBot(bool isBot)
    {
        if (!HasStateAuthority)
            return;

        IsBot = isBot;
    }

    public void SetConnectionId(string connectionId)
    {
        if (!HasStateAuthority)
            return;

        ConnectionId = connectionId;
    }

    public void ApplyRandomKillReward()
    {
        if (!HasStateAuthority || !NetworkRoundManager.IsGameplayActive)
            return;

        int rewardIndex = Random.Range(0, 3);

        switch (rewardIndex)
        {
            case 0:
                CurrentMoveSpeed =
                    Mathf.Min(CurrentMoveSpeed + MoveSpeedReward, MaxMoveSpeed);
                break;
            case 1:
                CurrentKnockbackPower =
                    Mathf.Min(CurrentKnockbackPower + KnockbackPowerReward, MaxKnockbackPower);
                break;
            default:
                CurrentAttackRange =
                    Mathf.Min(CurrentAttackRange + AttackRangeReward, MaxAttackRange);
                break;
        }

        ApplyRuntimeStats();
    }

    private void ApplyRuntimeStats()
    {
        NetworkPlayerMotor motor = GetComponent<NetworkPlayerMotor>();
        BatAttack batAttack = GetComponent<BatAttack>();

        if (motor != null && CurrentMoveSpeed > 0f)
            motor.moveSpeed = CurrentMoveSpeed;

        if (batAttack != null)
        {
            if (CurrentKnockbackPower > 0f)
                batAttack.knockbackPower = CurrentKnockbackPower;

            if (CurrentAttackRange > 0f)
                batAttack.attackRange = CurrentAttackRange;
        }
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

            ConfigureKillScale(character, capsuleVisual);
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

        currentModel.transform.localPosition = character.modelPosition;
        currentModel.transform.localRotation =
            Quaternion.Euler(character.modelRotation);
        currentModel.transform.localScale = character.modelScale;

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

        ConfigureKillScale(character, currentModel);
        appliedCharacterId = character.id;
    }

    private void ConfigureKillScale(CharacterData character, GameObject visualObject)
    {
        NetworkKillScaleVisual scaleVisual = GetKillScaleVisual();
        scaleVisual.Configure(character, visualObject);
    }

    private void ApplyKillScale()
    {
        if (killScaleVisual != null)
            killScaleVisual.ApplyCurrentScale();
    }

    private NetworkKillScaleVisual GetKillScaleVisual()
    {
        if (killScaleVisual == null)
            killScaleVisual = GetComponent<NetworkKillScaleVisual>();

        if (killScaleVisual == null)
            killScaleVisual = gameObject.AddComponent<NetworkKillScaleVisual>();

        return killScaleVisual;
    }
}
