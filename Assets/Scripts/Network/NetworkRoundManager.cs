using System.Collections.Generic;
using Fusion;
using UnityEngine;

public enum RoundPhase
{
    Waiting,
    CharacterSelect,
    Playing,
    Result
}

public struct RoundResultEntry : INetworkStruct
{
    public PlayerRef Player;
    public NetworkBool IsBot;
    public NetworkString<_16> Nickname;
    public NetworkString<_32> ConnectionId;
    public int KillCount;
}

public class NetworkRoundManager : NetworkBehaviour
{
    private const int ResultCapacity = 16;

    public static NetworkRoundManager Instance { get; private set; }
    public static bool IsGameplayActive =>
        Instance != null && Instance.Phase == RoundPhase.Playing;

    [Header("Round Durations")]
    [SerializeField, Min(0.1f)] private float characterSelectDuration = 15f;
    [SerializeField, Min(0.1f)] private float playingDuration = 180f;
    [SerializeField, Min(0.1f)] private float resultDuration = 7f;

    [Networked]
    public RoundPhase Phase { get; private set; }

    [Networked]
    public TickTimer PhaseTimer { get; private set; }

    [Networked]
    public int RoundNumber { get; private set; }

    [Networked]
    public int ResultEntryCount { get; private set; }

    [Networked, Capacity(ResultCapacity)]
    public NetworkArray<RoundResultEntry> ResultEntries => default;

    private RoundPhase presentedPhase = (RoundPhase)(-1);
    private int presentedRoundNumber = -1;

    public override void Spawned()
    {
        Instance = this;
        RefreshLocalPresentation();
    }

    public override void Despawned(NetworkRunner networkRunner, bool hasState)
    {
        if (Instance == this)
            Instance = null;
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority || !PhaseTimer.Expired(Runner))
            return;

        switch (Phase)
        {
            case RoundPhase.CharacterSelect:
                StartPlaying();
                break;

            case RoundPhase.Playing:
                StartResult();
                break;

            case RoundPhase.Result:
                StartNextRound();
                break;
        }
    }

    public override void Render()
    {
        if (presentedPhase == Phase && presentedRoundNumber == RoundNumber)
            return;

        ApplyLocalPresentation();
    }

    public void TryStartFirstRound()
    {
        if (!HasStateAuthority ||
            Phase != RoundPhase.Waiting ||
            RoundNumber != 0)
        {
            return;
        }

        RoundNumber = 1;
        Phase = RoundPhase.CharacterSelect;
        PhaseTimer = TickTimer.CreateFromSeconds(
            Runner,
            characterSelectDuration
        );

        NetworkGameManager.Instance?.HandleRoundPhaseStarted(Phase);
        Debug.Log($"[Round] Round {RoundNumber} CharacterSelect started");
    }

    public void RefreshLocalPresentation()
    {
        presentedPhase = (RoundPhase)(-1);
        presentedRoundNumber = -1;
        ApplyLocalPresentation();
    }

    public float GetRemainingTime()
    {
        if (Runner == null || !Runner.IsRunning || !PhaseTimer.IsRunning)
            return 0f;

        float? remainingTime = PhaseTimer.RemainingTime(Runner);
        return Mathf.Max(0f, remainingTime ?? 0f);
    }

    public bool TryGetResultEntry(int index, out RoundResultEntry resultEntry)
    {
        if (index < 0 || index >= ResultEntryCount)
        {
            resultEntry = default;
            return false;
        }

        resultEntry = ResultEntries[index];
        return true;
    }

    private void StartPlaying()
    {
        Phase = RoundPhase.Playing;
        PhaseTimer = TickTimer.CreateFromSeconds(Runner, playingDuration);

        NetworkGameManager.Instance?.HandleRoundPhaseStarted(Phase);
        Debug.Log($"[Round] Round {RoundNumber} Playing started");
    }

    private void StartResult()
    {
        Phase = RoundPhase.Result;
        PhaseTimer = TickTimer.CreateFromSeconds(Runner, resultDuration);

        CaptureRoundResults();
        NetworkGameManager.Instance?.HandleRoundPhaseStarted(Phase);
        Debug.Log($"[Round] Round {RoundNumber} Result started");
    }

    private void StartNextRound()
    {
        ClearRoundResults();
        RoundNumber++;
        Phase = RoundPhase.CharacterSelect;
        PhaseTimer = TickTimer.CreateFromSeconds(
            Runner,
            characterSelectDuration
        );

        NetworkGameManager.Instance?.HandleRoundPhaseStarted(Phase);
        Debug.Log($"[Round] Round {RoundNumber} CharacterSelect started");
    }

    private void CaptureRoundResults()
    {
        List<RoundResultEntry> results = new();
        NetworkPlayerScore[] playerScores =
            FindObjectsByType<NetworkPlayerScore>(FindObjectsSortMode.None);

        foreach (NetworkPlayerScore playerScore in playerScores)
        {
            if (playerScore == null ||
                playerScore.Object == null ||
                !playerScore.Object.IsValid)
            {
                continue;
            }

            NetworkPlayerName playerName =
                playerScore.GetComponent<NetworkPlayerName>();

            NetworkPlayerStats playerStats =
                playerScore.GetComponent<NetworkPlayerStats>();

            bool isBot = playerStats != null && playerStats.IsBot;
            string nickname = playerName != null
                ? playerName.Nickname.ToString()
                : "";

            if (string.IsNullOrEmpty(nickname))
            {
                nickname = isBot
                    ? "Bot"
                    : $"Player {playerScore.Object.InputAuthority.PlayerId}";
            }

            results.Add(new RoundResultEntry
            {
                Player = playerScore.Object.InputAuthority,
                IsBot = isBot,
                Nickname = nickname,
                ConnectionId = playerStats != null
                    ? playerStats.ConnectionId
                    : default,
                KillCount = playerScore.KillCount
            });
        }

        results.Sort(CompareResultEntries);
        ClearRoundResults();

        int resultCount = Mathf.Min(results.Count, ResultCapacity);

        for (int index = 0; index < resultCount; index++)
            ResultEntries.Set(index, results[index]);

        ResultEntryCount = resultCount;

        if (results.Count > ResultCapacity)
        {
            Debug.LogWarning(
                $"[Round] Result entries truncated to {ResultCapacity}."
            );
        }
    }

    private void ClearRoundResults()
    {
        int previousResultCount = ResultEntryCount;
        ResultEntryCount = 0;

        for (int index = 0; index < previousResultCount; index++)
            ResultEntries.Set(index, default);
    }

    private static int CompareResultEntries(
        RoundResultEntry left,
        RoundResultEntry right
    )
    {
        int killComparison = right.KillCount.CompareTo(left.KillCount);

        if (killComparison != 0)
            return killComparison;

        int leftBotOrder = left.IsBot ? 1 : 0;
        int rightBotOrder = right.IsBot ? 1 : 0;
        int botComparison = leftBotOrder.CompareTo(rightBotOrder);

        if (botComparison != 0)
            return botComparison;

        int playerComparison =
            left.Player.PlayerId.CompareTo(right.Player.PlayerId);

        if (playerComparison != 0)
            return playerComparison;

        return string.CompareOrdinal(
            left.Nickname.ToString(),
            right.Nickname.ToString()
        );
    }

    private void ApplyLocalPresentation()
    {
        CharacterSelectUI characterSelectUI =
            FindFirstObjectByType<CharacterSelectUI>();

        if (characterSelectUI == null)
            return;

        switch (Phase)
        {
            case RoundPhase.CharacterSelect:
                characterSelectUI.BeginCharacterSelection(RoundNumber);
                break;

            case RoundPhase.Playing:
                NetworkPlayerCommand localCommand =
                    NetworkRunnerManager.Instance != null
                        ? NetworkRunnerManager.Instance.LocalCommand
                        : null;

                if (localCommand == null || !localCommand.HasSelectedCharacter)
                    characterSelectUI.Show();
                else
                    characterSelectUI.Hide();
                break;

            default:
                characterSelectUI.Hide();
                break;
        }

        presentedPhase = Phase;
        presentedRoundNumber = RoundNumber;
    }
}
