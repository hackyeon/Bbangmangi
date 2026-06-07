using UnityEngine;

[CreateAssetMenu(
    fileName = "CharacterData",
    menuName = "Bbangmangi/Character Data"
)]
public class CharacterData : ScriptableObject
{
    public int id;
    public string characterName;

    [Header("Stats")]
    public float moveSpeed = 6f;
    public float knockbackPower = 26f;

    [Header("Prefab")]
    public GameObject modelPrefab;

    [Header("Gameplay Visual")]
    public Vector3 modelPosition = new Vector3(0f, -1f, 0f);
    public Vector3 modelRotation = Vector3.zero;
    public Vector3 modelScale = Vector3.one;

    [Header("Capsule Character")]
    public bool useCapsuleVisual;

    [Header("UI")]
    public Sprite previewImage;

    [Header("3D Preview")]
    public Vector3 previewPosition = Vector3.zero;
    public Vector3 previewRotation = Vector3.zero;
    public Vector3 previewScale = Vector3.one;
}
