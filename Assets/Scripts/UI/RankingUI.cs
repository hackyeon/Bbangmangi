using System.Collections.Generic;
using System.Linq;
using Fusion;
using TMPro;
using UnityEngine;

public class RankingUI : MonoBehaviour
{
    public TMP_Text rankingText;

    private NetworkRunner runner;

    private void Start()
    {
        runner = FindFirstObjectByType<NetworkRunner>();
    }

    private void Update()
    {
        if (rankingText == null)
            return;

        if (runner == null)
            runner = FindFirstObjectByType<NetworkRunner>();

        NetworkPlayerScore[] players =
            FindObjectsByType<NetworkPlayerScore>(
                FindObjectsSortMode.None
            );

        List<NetworkPlayerScore> ranking =
            players
                .Where(IsValidPlayer)
                .OrderByDescending(player => player.KillCount)
                .ThenBy(player => player.Object.InputAuthority.PlayerId)
                .ToList();

        PlayerRef localPlayer =
            runner != null ? runner.LocalPlayer : PlayerRef.None;

        int myIndex = ranking.FindIndex(
            player => player.Object.InputAuthority == localPlayer
        );

        string text = "";

        int topCount = Mathf.Min(5, ranking.Count);

        for (int i = 0; i < topCount; i++)
        {
            text += BuildLine(i, ranking[i], localPlayer);
        }

        if (myIndex >= 5)
        {
            text += "...\n";
            text += BuildLine(myIndex, ranking[myIndex], localPlayer);
        }

        rankingText.text = text;
    }

    private bool IsValidPlayer(NetworkPlayerScore player)
    {
        if (player == null)
            return false;

        if (player.Object == null)
            return false;

        if (!player.Object.IsValid)
            return false;

        return true;
    }

    private string BuildLine(
        int index,
        NetworkPlayerScore player,
        PlayerRef localPlayer
    )
    {
        string nickname = GetNickname(player);
        int rank = index + 1;
        int killCount = player.KillCount;

        string line =
            $"{rank}. {nickname}  {killCount}\n";

        if (player.Object.InputAuthority == localPlayer)
            return $"<color=#5DFFB5>{line}</color>";

        return line;
    }

    private string GetNickname(NetworkPlayerScore player)
    {
        NetworkPlayerName playerName =
            player.GetComponent<NetworkPlayerName>();

        if (playerName == null)
            return $"Player {player.Object.InputAuthority.PlayerId}";

        string nickname = playerName.Nickname.ToString();

        if (string.IsNullOrEmpty(nickname))
            return $"Player {player.Object.InputAuthority.PlayerId}";

        return nickname;
    }
}