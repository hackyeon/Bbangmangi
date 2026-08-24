using TMPro;
using UnityEngine;

public class RoundTimerUI : MonoBehaviour
{
    [Header("Character Select")]
    public TMP_Text characterSelectRoundText;
    public TMP_Text characterSelectTimerText;

    [Header("Playing")]
    public GameObject playingTimerRoot;
    public TMP_Text playingRoundText;
    public TMP_Text playingTimerText;

    private RoundPhase displayedPhase = (RoundPhase)(-1);
    private int displayedRoundNumber = -1;
    private int previousDisplayedSecond = -1;

    private void Start()
    {
        SetCharacterSelectVisible(false);
        SetPlayingVisible(false);
    }

    private void Update()
    {
        NetworkRoundManager roundManager = NetworkRoundManager.Instance;

        if (roundManager == null)
            return;

        if (displayedPhase != roundManager.Phase ||
            displayedRoundNumber != roundManager.RoundNumber)
        {
            ApplyPhase(roundManager);
        }

        if (roundManager.Phase != RoundPhase.CharacterSelect &&
            roundManager.Phase != RoundPhase.Playing)
        {
            return;
        }

        int remainingSecond = Mathf.CeilToInt(roundManager.GetRemainingTime());

        if (previousDisplayedSecond == remainingSecond)
            return;

        previousDisplayedSecond = remainingSecond;
        string formattedTime = FormatTime(remainingSecond);

        if (roundManager.Phase == RoundPhase.CharacterSelect)
        {
            if (characterSelectTimerText != null)
            {
                characterSelectTimerText.text =
                    $"게임 시작까지\n{formattedTime}";
            }
        }
        else if (playingTimerText != null)
        {
            playingTimerText.text = formattedTime;
        }
    }

    private void ApplyPhase(NetworkRoundManager roundManager)
    {
        displayedPhase = roundManager.Phase;
        displayedRoundNumber = roundManager.RoundNumber;
        previousDisplayedSecond = -1;

        bool isCharacterSelect =
            roundManager.Phase == RoundPhase.CharacterSelect;

        bool isPlaying = roundManager.Phase == RoundPhase.Playing;

        SetCharacterSelectVisible(isCharacterSelect);
        SetPlayingVisible(isPlaying);

        if (characterSelectRoundText != null)
        {
            characterSelectRoundText.text =
                $"ROUND {roundManager.RoundNumber}";
        }

        if (playingRoundText != null)
            playingRoundText.text = $"ROUND {roundManager.RoundNumber}";
    }

    private void SetCharacterSelectVisible(bool visible)
    {
        SetActive(characterSelectRoundText, visible);
        SetActive(characterSelectTimerText, visible);
    }

    private void SetPlayingVisible(bool visible)
    {
        if (playingTimerRoot != null)
        {
            if (playingTimerRoot.activeSelf != visible)
                playingTimerRoot.SetActive(visible);

            return;
        }

        SetActive(playingRoundText, visible);
        SetActive(playingTimerText, visible);
    }

    private static void SetActive(TMP_Text text, bool visible)
    {
        if (text != null && text.gameObject.activeSelf != visible)
            text.gameObject.SetActive(visible);
    }

    private static string FormatTime(int totalSeconds)
    {
        totalSeconds = Mathf.Max(0, totalSeconds);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        return $"{minutes:00}:{seconds:00}";
    }
}
