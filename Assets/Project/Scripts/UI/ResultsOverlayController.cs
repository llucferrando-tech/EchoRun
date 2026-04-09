using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EchoRun.Core;
using EchoRun.Gameplay;
using EchoRun.Level;

namespace EchoRun.UI
{
    public sealed class ResultsOverlayController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameManager gameManager;
        [SerializeField] private ScoreManager scoreManager;
        [SerializeField] private LevelSession levelSession;

        [Header("UI")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text highScoreText;

        [Header("Buttons")]
        [SerializeField] private Button retryButton;
        [SerializeField] private Button backButton;

        private void Awake()
        {
            if (gameManager == null)
                gameManager = FindFirstObjectByType<GameManager>();

            if (scoreManager == null)
                scoreManager = FindFirstObjectByType<ScoreManager>();

            if (levelSession == null)
                levelSession = FindFirstObjectByType<LevelSession>();
        }

        private void OnEnable()
        {
            GameSignals.RunEnded += Refresh;

            if (retryButton != null)
                retryButton.onClick.AddListener(HandleRetry);

            if (backButton != null)
                backButton.onClick.AddListener(HandleBackToSongSelect);
        }

        private void OnDisable()
        {
            GameSignals.RunEnded -= Refresh;

            if (retryButton != null)
                retryButton.onClick.RemoveListener(HandleRetry);

            if (backButton != null)
                backButton.onClick.RemoveListener(HandleBackToSongSelect);
        }

        private void Refresh()
        {
            if (gameManager == null || scoreManager == null)
                return;

            UpdateTitle();
            UpdateScore();
            UpdateHighScore();
        }

        private void UpdateTitle()
        {
            if (titleText == null || gameManager == null)
                return;

            if (gameManager.CurrentState == GameState.Victory)
            {
                titleText.text = "LEVEL COMPLETE";
            }
            else if (gameManager.CurrentState == GameState.Defeat)
            {
                titleText.text = "FAILED";
            }
        }

        private void UpdateScore()
        {
            if (scoreText == null || scoreManager == null)
                return;

            scoreText.text = scoreManager.Score.ToString();
        }

        private void UpdateHighScore()
        {
            if (highScoreText == null || levelSession == null)
                return;

            LevelDefinition level = levelSession.CurrentLevel;

            int highScore = ScorePersistence.GetHighScore(level);
            highScoreText.text = highScore.ToString();
        }

        private void HandleRetry()
        {
            GameSignals.RaiseRetryRequested();
            Debug.Log("Retry");
        }

        private void HandleBackToSongSelect()
        {
            GameSignals.RaiseBackToSongSelectRequested();
            Debug.Log("Back to");
        }
    }
}