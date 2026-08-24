using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fusion;
using TMPro;
using UnityEngine;

public class RankingUI : MonoBehaviour
{
    [Header("Live Ranking")]
    public GameObject liveRankingRoot;
    public TMP_Text rankingText;

    [Header("Round Result")]
    public GameObject resultPanel;
    public TMP_Text resultTitleText;
    public TMP_Text resultTopThreeText;
    public TMP_Text resultMyRankText;
    public TMP_Text resultRemainingText;

    private NetworkRunner runner;
    private RoundPhase displayedPhase = (RoundPhase)(-1);
    private int displayedResultRound = -1;
    private int displayedResultCount = -1;
    private int previousDisplayedResultSecond = -1;

    private void Start()
    {
        runner = FindFirstObjectByType<NetworkRunner>();
        ApplyPhaseVisibility(RoundPhase.Waiting);
    }

    private void Update()
    {
        NetworkRoundManager roundManager = NetworkRoundManager.Instance;

        if (roundManager == null)
            return;

        if (runner == null)
            runner = FindFirstObjectByType<NetworkRunner>();

        if (displayedPhase != roundManager.Phase)
            ApplyPhaseVisibility(roundManager.Phase);

        switch (roundManager.Phase)
        {
            case RoundPhase.Playing:
                UpdateLiveRanking();
                break;

            case RoundPhase.Result:
                UpdateResult(roundManager);
                break;
        }
    }

    private void ApplyPhaseVisibility(RoundPhase phase)
    {
        SetLiveRankingVisible(phase == RoundPhase.Playing);

        if (resultPanel != null)
            resultPanel.SetActive(phase == RoundPhase.Result);

        displayedPhase = phase;

        if (phase == RoundPhase.Result)
        {
            displayedResultRound = -1;
            displayedResultCount = -1;
            previousDisplayedResultSecond = -1;
        }
    }

    private void UpdateLiveRanking()
    {
        if (rankingText == null)
            return;

        NetworkPlayerScore[] players =
            FindObjectsByType<NetworkPlayerScore>(FindObjectsSortMode.None);

        List<NetworkPlayerScore> ranking =
            players
                .Where(IsValidPlayer)
                .OrderByDescending(player => player.KillCount)
                .ThenBy(player => IsBot(player) ? 1 : 0)
                .ThenBy(player => player.Object.InputAuthority.PlayerId)
                .ThenBy(GetNickname)
                .ToList();

        int localPlayerIndex = ranking.FindIndex(IsLocalPlayer);
        StringBuilder rankingBuilder = new();
        int topCount = Mathf.Min(5, ranking.Count);

        for (int index = 0; index < topCount; index++)
            rankingBuilder.Append(BuildLiveLine(index, ranking[index]));

        if (localPlayerIndex >= 5)
        {
            rankingBuilder.Append("...\n");
            rankingBuilder.Append(
                BuildLiveLine(localPlayerIndex, ranking[localPlayerIndex])
            );
        }

        string rankingString = rankingBuilder.ToString();

        if (rankingText.text != rankingString)
            rankingText.text = rankingString;
    }

    private void UpdateResult(NetworkRoundManager roundManager)
    {
        if (displayedResultRound != roundManager.RoundNumber ||
            displayedResultCount != roundManager.ResultEntryCount)
        {
            RebuildResult(roundManager);
        }

        int remainingSecond = Mathf.CeilToInt(roundManager.GetRemainingTime());

        if (previousDisplayedResultSecond == remainingSecond)
            return;

        previousDisplayedResultSecond = remainingSecond;

        if (resultRemainingText != null)
            resultRemainingText.text = $"다음 게임까지 {remainingSecond}초";
    }

    private void RebuildResult(NetworkRoundManager roundManager)
    {
        displayedResultRound = roundManager.RoundNumber;
        displayedResultCount = roundManager.ResultEntryCount;

        if (resultTitleText != null)
            resultTitleText.text = $"ROUND {roundManager.RoundNumber} RESULT";

        StringBuilder topThreeBuilder = new();
        int topCount = Mathf.Min(3, roundManager.ResultEntryCount);

        for (int index = 0; index < topCount; index++)
        {
            if (!roundManager.TryGetResultEntry(index, out RoundResultEntry entry))
                continue;

            topThreeBuilder.AppendLine(
                $"{index + 1}위   {GetNickname(entry)}   {entry.KillCount} Kill"
            );
        }

        if (topThreeBuilder.Length == 0)
            topThreeBuilder.Append("결과가 없습니다.");

        if (resultTopThreeText != null)
            resultTopThreeText.text = topThreeBuilder.ToString().TrimEnd();

        UpdateLocalResult(roundManager);
    }

    private void UpdateLocalResult(NetworkRoundManager roundManager)
    {
        if (resultMyRankText == null)
            return;

        for (int index = 0; index < roundManager.ResultEntryCount; index++)
        {
            if (!roundManager.TryGetResultEntry(index, out RoundResultEntry entry))
                continue;

            if (!IsLocalPlayer(entry))
                continue;

            resultMyRankText.text =
                $"내 순위\n{index + 1}위 / {roundManager.ResultEntryCount}명\n" +
                $"{entry.KillCount} Kill";
            return;
        }

        resultMyRankText.text = "내 순위\n이번 라운드 미참가";
    }

    private string BuildLiveLine(int index, NetworkPlayerScore player)
    {
        string line =
            $"{index + 1}. {GetNickname(player)}  {player.KillCount}\n";

        return IsLocalPlayer(player)
            ? $"<color=#5DFFB5>{line}</color>"
            : line;
    }

    private bool IsLocalPlayer(NetworkPlayerScore player)
    {
        NetworkPlayerStats playerStats =
            player.GetComponent<NetworkPlayerStats>();

        string connectionId = playerStats != null
            ? playerStats.ConnectionId.ToString()
            : "";

        if (!string.IsNullOrEmpty(connectionId))
            return connectionId == NetworkRunnerManager.LocalConnectionId;

        return runner != null &&
               player.Object.InputAuthority == runner.LocalPlayer;
    }

    private bool IsLocalPlayer(RoundResultEntry entry)
    {
        if (entry.IsBot)
            return false;

        string connectionId = entry.ConnectionId.ToString();

        if (!string.IsNullOrEmpty(connectionId))
            return connectionId == NetworkRunnerManager.LocalConnectionId;

        return runner != null && entry.Player == runner.LocalPlayer;
    }

    private static bool IsValidPlayer(NetworkPlayerScore player)
    {
        return player != null &&
               player.Object != null &&
               player.Object.IsValid;
    }

    private static bool IsBot(NetworkPlayerScore player)
    {
        NetworkPlayerStats playerStats =
            player.GetComponent<NetworkPlayerStats>();

        return playerStats != null && playerStats.IsBot;
    }

    private static string GetNickname(NetworkPlayerScore player)
    {
        NetworkPlayerName playerName =
            player.GetComponent<NetworkPlayerName>();

        string nickname = playerName != null
            ? playerName.Nickname.ToString()
            : "";

        if (!string.IsNullOrEmpty(nickname))
            return nickname;

        return IsBot(player)
            ? "Bot"
            : $"Player {player.Object.InputAuthority.PlayerId}";
    }

    private static string GetNickname(RoundResultEntry entry)
    {
        string nickname = entry.Nickname.ToString();

        if (!string.IsNullOrEmpty(nickname))
            return nickname;

        return entry.IsBot
            ? "Bot"
            : $"Player {entry.Player.PlayerId}";
    }

    private void SetLiveRankingVisible(bool visible)
    {
        GameObject target = liveRankingRoot != null
            ? liveRankingRoot
            : rankingText != null
                ? rankingText.gameObject
                : null;

        if (target != null && target.activeSelf != visible)
            target.SetActive(visible);
    }
}
