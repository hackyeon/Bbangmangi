using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public int playerScore;
    public int enemyScore;
    public int winScore = 3;

    public Text playerScoreText;
    public Text enemyScoreText;
    public Text resultText;
    public Button restartButton;

    void Start()
    {
        Time.timeScale = 1f;
        UpdateUI();

        if (resultText != null)
            resultText.gameObject.SetActive(false);

        if (restartButton != null)
            restartButton.gameObject.SetActive(false);
    }

    public void AddPlayerScore()
    {
        playerScore++;
        UpdateUI();
        CheckGameEnd();
    }

    public void AddEnemyScore()
    {
        enemyScore++;
        UpdateUI();
        CheckGameEnd();
    }

    private void UpdateUI()
    {
        playerScoreText.text = $"Player: {playerScore}";
        enemyScoreText.text = $"Enemy: {enemyScore}";
    }

    private void CheckGameEnd()
    {
        if (playerScore >= winScore)
        {
            ShowResult("WIN!");
        }
        else if (enemyScore >= winScore)
        {
            ShowResult("LOSE...");
        }
    }

    private void ShowResult(string message)
    {
        if (resultText != null)
        {
            resultText.gameObject.SetActive(true);
            resultText.text = message;
        }

        if (restartButton != null)
            restartButton.gameObject.SetActive(true);

        Time.timeScale = 0f;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}