using System.Collections.Generic;
using System.Linq;
using Fusion;
using UnityEngine;
using UnityEngine.UI;

public class RankingUI : MonoBehaviour
{
    public Text rankingText;

    private void Update()
    {
        if (rankingText == null)
            return;

        NetworkPlayerScore[] players =
            FindObjectsByType<NetworkPlayerScore>(
                FindObjectsSortMode.None
            );

        List<NetworkPlayerScore> validPlayers = new();

        foreach (NetworkPlayerScore player in players)
        {
            if (player == null)
                continue;

            if (player.Object == null)
                continue;

            if (!player.Object.IsValid)
                continue;

            validPlayers.Add(player);
        }

        List<NetworkPlayerScore> ranking =
            validPlayers
                .OrderByDescending(player => player.KillCount)
                .ToList();

        string text = "Ranking\n";

        for (int i = 0; i < ranking.Count; i++)
        {
            PlayerRef playerRef =
                ranking[i].Object.InputAuthority;

            text += $"{i + 1}. Player {playerRef.PlayerId} : {ranking[i].KillCount}\n";
        }

        rankingText.text = text;
    }
}