using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterButtonUI : MonoBehaviour
{
    public Button button;

    [Header("Background")]
    public Image backgroundImage;
    public Sprite normalBackgroundSprite;
    public Sprite selectedBackgroundSprite;

    [Header("Preview")]
    public Image previewImage;

    [Header("Info")]
    public TMP_Text nameText;

    [Header("Stats")]
    public Image speedFillImage;
    public Image powerFillImage;

    [Header("Text Colors")]
    public Color normalTextColor = Color.white;
    public Color selectedTextColor = new Color32(0x5D, 0xFF, 0xB5, 0xFF);

    private const float MaxSpeed = 10f;
    private const float MaxPower = 50f;

    private CharacterData characterData;
    private CharacterSelectUI owner;

    public CharacterData CharacterData => characterData;

    public void Bind(CharacterData data, CharacterSelectUI selectUI)
    {
        characterData = data;
        owner = selectUI;

        if (previewImage != null)
        {
            previewImage.sprite = data.previewImage;
            previewImage.preserveAspect = true;
        }

        if (nameText != null)
            nameText.text = data.characterName;

        SetProgress(speedFillImage, data.moveSpeed, MaxSpeed);
        SetProgress(powerFillImage, data.knockbackPower, MaxPower);

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClick);
        }

        SetSelected(false);
    }

    public void SetSelected(bool selected)
    {
        if (backgroundImage != null)
        {
            backgroundImage.sprite =
                selected
                    ? selectedBackgroundSprite
                    : normalBackgroundSprite;

            backgroundImage.color = Color.white;
        }

        if (nameText != null)
            nameText.color = selected ? selectedTextColor : normalTextColor;
    }

    private void SetProgress(Image fillImage, float value, float maxValue)
    {
        if (fillImage == null)
            return;

        fillImage.fillAmount = Mathf.Clamp01(value / maxValue);
    }

    private void OnClick()
    {
        owner.SelectCharacter(characterData, this);
    }
}