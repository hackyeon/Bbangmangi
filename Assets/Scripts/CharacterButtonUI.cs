using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterButtonUI : MonoBehaviour
{
    public Image iconImage;
    public TMP_Text nameText;
    public Button button;

    private CharacterData characterData;
    private CharacterSelectUI owner;

    public void Bind(CharacterData data, CharacterSelectUI selectUI)
    {
        characterData = data;
        owner = selectUI;

        if (iconImage != null)
            iconImage.sprite = data.icon;

        if (nameText != null)
            nameText.text = data.characterName;

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClick);
        }
    }

    private void OnClick()
    {
        owner.SelectCharacter(characterData);
    }
}