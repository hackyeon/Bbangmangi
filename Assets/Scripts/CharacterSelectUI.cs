using UnityEngine;
using UnityEngine.UI;

public class CharacterSelectUI : MonoBehaviour
{
    public GameObject panel;
    public InputField nameInputField;
    public Button startButton;

    private NetworkRunnerManager networkRunnerManager;

    private void Start()
    {
        networkRunnerManager = FindFirstObjectByType<NetworkRunnerManager>();

        if (startButton != null)
        {
            startButton.interactable = false;
            startButton.onClick.AddListener(OnClickStart);
        }

        Show();
    }

    private void OnClickStart()
    {
        string nickname = "Player";

        if (nameInputField != null &&
            !string.IsNullOrWhiteSpace(nameInputField.text))
        {
            nickname = nameInputField.text.Trim();
        }

        networkRunnerManager.RequestSpawn(nickname);
        Hide();
    }

    public void SetStartButtonEnabled(bool enabled)
    {
        if (startButton != null)
            startButton.interactable = enabled;
    }

    public void Show()
    {
        if (panel != null)
            panel.SetActive(true);
    }

    public void Hide()
    {
        if (panel != null)
            panel.SetActive(false);
    }
}