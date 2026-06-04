using Fusion;
using TMPro;
using UnityEngine;

public class NetworkPlayerName : NetworkBehaviour
{
    public TMP_Text nameText;

    private readonly Color myNameColor =
        new Color32(0x5D, 0xFF, 0xB5, 0xFF);

    private Color defaultNameColor;

    [Networked]
    public NetworkString<_16> Nickname { get; set; }

    public override void Spawned()
    {
        if (nameText != null)
            defaultNameColor = nameText.color;

        UpdateNameText();
    }

    public override void FixedUpdateNetwork()
    {
        UpdateNameText();
    }

    public void SetNickname(string nickname)
    {
        if (!HasStateAuthority)
            return;

        Nickname = nickname;
    }

    private void UpdateNameText()
    {
        if (nameText == null)
            return;

        nameText.text = Nickname.ToString();

        nameText.color =
            HasInputAuthority
                ? myNameColor
                : defaultNameColor;
    }
}