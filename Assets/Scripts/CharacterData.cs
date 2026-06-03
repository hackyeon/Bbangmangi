using UnityEngine;

[CreateAssetMenu(
    fileName = "CharacterData",
    menuName = "Bbangmangi/Character Data"
)]
public class CharacterData : ScriptableObject
{
    public int id;
    public string characterName;

    public float moveSpeed = 6f;
    public float knockbackPower = 26f;

    public Sprite icon;
    public GameObject modelPrefab;
}