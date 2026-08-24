using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterSelectUI : MonoBehaviour
{
    public GameObject panel;
    public TMP_InputField nameInputField;
    public Button startButton;
    public TMP_Text startButtonText;
    public TMP_Text messageText;

    public CharacterData[] characters;
    public Transform characterButtonParent;
    public CharacterButtonUI characterButtonPrefab;

    [SerializeField]
    private GameObject gameUI;

    private CharacterData selectedCharacter;
    private NetworkRunnerManager networkRunnerManager;
    private bool isNetworkReady;
    private string lastNickname;
    private int preparedRoundNumber = -1;

    private void Start()
    {
        networkRunnerManager = FindFirstObjectByType<NetworkRunnerManager>();

        if (startButton != null)
        {
            startButton.interactable = false;
            startButton.onClick.AddListener(OnClickStart);
        }

        if (startButtonText != null)
            startButtonText.text = "연결 중...";

        if (nameInputField != null)
        {
            nameInputField.characterLimit = 12;
            nameInputField.onValueChanged.AddListener(_ => ValidateInput());
        }

        CreateCharacterButtons();

        selectedCharacter = null;

        Show();
    }

    private void CreateCharacterButtons()
    {
        if (characters == null ||
            characterButtonParent == null ||
            characterButtonPrefab == null)
            return;

        foreach (Transform child in characterButtonParent)
        {
            Destroy(child.gameObject);
        }

        foreach (CharacterData character in characters)
        {
            CharacterButtonUI button =
                Instantiate(characterButtonPrefab, characterButtonParent);

            button.Bind(character, this);
        }
    }

    public void SelectCharacter(
        CharacterData character,
        CharacterButtonUI selectedButton
    )
    {
        selectedCharacter = character;

        CharacterButtonUI[] buttons =
            characterButtonParent.GetComponentsInChildren<CharacterButtonUI>();

        foreach (CharacterButtonUI button in buttons)
        {
            button.SetSelected(button == selectedButton);
        }

        ValidateInput();
    }

    private void OnClickStart()
    {
        if (!ValidateInput())
            return;

        string nickname = nameInputField.text.Trim();
        lastNickname = nickname;

        networkRunnerManager.SubmitCharacterSelection(
            nickname,
            selectedCharacter.id
        );

        if (NetworkRoundManager.Instance != null &&
            NetworkRoundManager.Instance.Phase == RoundPhase.Playing)
        {
            Hide();
            return;
        }

        if (startButtonText != null)
            startButtonText.text = "선택 변경";

        SetMessage("캐릭터 선택이 저장되었습니다.");
    }

    public void SetStartButtonEnabled(bool enabled)
    {
        isNetworkReady = enabled;
        ValidateInput();
    }

    public void Show()
    {
        if (panel != null)
            panel.SetActive(true);

        if (gameUI != null)
            gameUI.SetActive(false);

        if (nameInputField != null)
            lastNickname = nameInputField.text.Trim();

        ValidateInput();
    }

    public void BeginCharacterSelection(int roundNumber)
    {
        if (preparedRoundNumber != roundNumber)
        {
            preparedRoundNumber = roundNumber;
            selectedCharacter = null;

            if (characterButtonParent != null)
            {
                CharacterButtonUI[] buttons =
                    characterButtonParent.GetComponentsInChildren<CharacterButtonUI>();

                foreach (CharacterButtonUI button in buttons)
                    button.SetSelected(false);
            }
        }

        Show();
    }

    public void Hide()
    {
        if (panel != null)
            panel.SetActive(false);

        if (gameUI != null)
            gameUI.SetActive(true);
    }

    public void ShowSelectionError(string message)
    {
        Show();
        SetMessage(message);
    }

    private bool ValidateInput()
    {
        if (startButton == null)
            return false;

        if (!isNetworkReady)
        {
            startButton.interactable = false;

            if (startButtonText != null)
                startButtonText.text = "연결 중...";

            SetMessage("");
            return false;
        }

        if (selectedCharacter == null)
        {
            startButton.interactable = false;

            if (startButtonText != null)
                startButtonText.text = "선택 완료";

            SetMessage("캐릭터를 선택해 주세요.");
            return false;
        }

        string nickname = "";

        if (nameInputField != null)
            nickname = nameInputField.text.Trim();

        if (string.IsNullOrEmpty(nickname))
        {
            startButton.interactable = false;

            if (startButtonText != null)
                startButtonText.text = "선택 완료";

            SetMessage("캐릭터 이름을 입력해 주세요.");
            return false;
        }

        if (IsDuplicateName(nickname))
        {
            startButton.interactable = false;

            if (startButtonText != null)
                startButtonText.text = "선택 완료";

            SetMessage("이미 사용 중인 이름입니다.");
            return false;
        }

        startButton.interactable = true;

        if (startButtonText != null)
            startButtonText.text = "선택 완료";

        SetMessage("");
        return true;
    }

    private bool IsDuplicateName(string nickname)
    {
        NetworkPlayerName[] players =
            FindObjectsByType<NetworkPlayerName>(
                FindObjectsSortMode.None
            );

        foreach (NetworkPlayerName player in players)
        {
            if (player == null)
                continue;

            string existingName = player.Nickname.ToString();

            if (string.Equals(
                    existingName,
                    nickname,
                    System.StringComparison.OrdinalIgnoreCase))
            {
                if (nickname == lastNickname)
                    continue;

                return true;
            }
        }

        NetworkPlayerCommand[] playerCommands =
            FindObjectsByType<NetworkPlayerCommand>(FindObjectsSortMode.None);

        foreach (NetworkPlayerCommand playerCommand in playerCommands)
        {
            if (playerCommand == null ||
                playerCommand.HasInputAuthority ||
                !playerCommand.HasSelectedCharacter)
            {
                continue;
            }

            if (string.Equals(
                    playerCommand.SelectedNickname.ToString(),
                    nickname,
                    System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private void SetMessage(string message)
    {
        if (messageText != null)
            messageText.text = message;
    }
}
