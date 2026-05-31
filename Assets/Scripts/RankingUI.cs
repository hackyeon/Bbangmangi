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
        NetworkPlayerScore[] players =
            FindObjectsOfType<NetworkPlayerScore>();

        List<NetworkPlayerScore> ranking =
            players
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