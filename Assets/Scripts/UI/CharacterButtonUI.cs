using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterButtonUI : MonoBehaviour
{
    [Header("Root")]
    public Button button;
    public Image backgroundImage;

    [Header("Preview")]
    public Image previewImage;
    public RawImage previewRawImage;

    [Header("Info")]
    public TMP_Text nameText;

    [Header("Stats")]
    public Image speedFillImage;
    public Image powerFillImage;

    [Header("Selected")]
    public GameObject selectedMark;

    [Header("Colors")]
    public Color normalColor = new Color32(20, 25, 45, 210);
    public Color selectedColor = new Color32(0x5D, 0xFF, 0xB5, 255);

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
            previewImage.sprite = data.previewImage;

        if (previewRawImage != null)
            previewRawImage.gameObject.SetActive(false);

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
        if (selectedMark != null)
            selectedMark.SetActive(selected);

        if (backgroundImage != null)
            backgroundImage.color = selected ? selectedColor : normalColor;

        if (previewImage != null)
            previewImage.gameObject.SetActive(!selected);

        if (previewRawImage != null)
            previewRawImage.gameObject.SetActive(selected);
    }

    public void SetPreviewTexture(Texture texture)
    {
        if (previewRawImage != null)
            previewRawImage.texture = texture;
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