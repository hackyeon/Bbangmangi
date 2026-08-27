using System.Collections.Generic;
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

    [SerializeField, Min(0.05f)]
    private float liveRefreshInterval = 0.2f;

    private NetworkRunner runner;
    private RoundPhase displayedPhase = (RoundPhase)(-1);
    private int displayedResultRound = -1;
    private int displayedResultCount = -1;
    private int previousDisplayedResultSecond = -1;
    private float nextLiveRefreshTime;
    private readonly List<NetworkPlayerScore> liveRanking = new();
    private readonly StringBuilder liveRankingBuilder = new();

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

        if (runner != roundManager.Runner)
            runner = roundManager.Runner;

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

        if (phase == RoundPhase.Playing)
            nextLiveRefreshTime = 0f;
    }

    private void UpdateLiveRanking()
    {
        if (rankingText == null)
            return;

        if (Time.unscaledTime < nextLiveRefreshTime)
            return;

        nextLiveRefreshTime = Time.unscaledTime + liveRefreshInterval;

        NetworkPlayerScore[] players =
            FindObjectsByType<NetworkPlayerScore>(FindObjectsSortMode.None);

        liveRanking.Clear();

        foreach (NetworkPlayerScore player in players)
        {
            if (IsValidPlayer(player))
                liveRanking.Add(player);
        }

        liveRanking.Sort(NetworkRoundManager.ComparePlayerScores);

        int localPlayerIndex = -1;

        for (int index = 0; index < liveRanking.Count; index++)
        {
            if (!IsLocalPlayer(liveRanking[index]))
                continue;

            localPlayerIndex = index;
            break;
        }

        liveRankingBuilder.Clear();
        int topCount = Mathf.Min(5, liveRanking.Count);

        for (int index = 0; index < topCount; index++)
        {
            liveRankingBuilder.Append(
                BuildLiveLine(index, liveRanking[index])
            );
        }

        if (localPlayerIndex >= 5)
        {
            liveRankingBuilder.Append("...\n");
            liveRankingBuilder.Append(
                BuildLiveLine(
                    localPlayerIndex,
                    liveRanking[localPlayerIndex]
                )
            );
        }

        string rankingString = liveRankingBuilder.ToString();

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
                $"{index + 1}위   {GetKingMarker(entry.IsKing)}" +
                $"{GetNickname(entry)}   {entry.RoundScore}점 / " +
                $"{entry.KillCount} Kill"
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
                $"{entry.RoundScore}점 / {entry.KillCount} Kill";
            return;
        }

        resultMyRankText.text = "내 순위\n이번 라운드 미참가";
    }

    private string BuildLiveLine(int index, NetworkPlayerScore player)
    {
        string line =
            $"{index + 1}. {GetKingMarker(player.IsKing)}" +
            $"{GetNickname(player)}  {player.RoundScore}점 · " +
            $"{player.KillCount} Kill\n";

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

    private bool IsValidPlayer(NetworkPlayerScore player)
    {
        return player != null &&
               player.Object != null &&
               player.Object.IsValid &&
               (runner == null || player.Runner == runner);
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

    private static string GetKingMarker(bool isKing)
    {
        return isKing ? "<color=#FFD34E>[KING]</color> " : "";
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
