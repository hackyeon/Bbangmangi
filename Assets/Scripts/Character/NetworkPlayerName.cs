using Fusion;
using TMPro;
using UnityEngine;

public class NetworkPlayerName : NetworkBehaviour
{
    public TMP_Text nameText;

    [Networked]
    public NetworkString<_16> Nickname { get; set; }

    public override void Spawned()
    {
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
        if (nameText != null)
            nameText.text = Nickname.ToString();
    }
}