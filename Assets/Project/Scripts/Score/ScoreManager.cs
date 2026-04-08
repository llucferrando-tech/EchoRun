using UnityEngine;
using EchoRun.Core;
using EchoRun.Level;
using EchoRun.UI;

namespace EchoRun.Gameplay
{
    public sealed class ScoreManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private LevelDefinition currentLevel;
        [SerializeField] private ScoreUIController scoreUIController;

        [Header("Tuning")]
        [SerializeField] private int pointsPerObstacle = 10;
        [SerializeField] private int nearMissBonus = 25;
        [SerializeField] private int goodBeatBonus = 5;
        [SerializeField] private int perfectBeatBonus = 10;

        public int Score { get; private set; }
        public int ObstaclesPassed { get; private set; }
        public int NearMissCount { get; private set; }
        public bool IsRunActive { get; private set; }

        private void Awake()
        {
            if (scoreUIController == null)
            {
                scoreUIController = FindFirstObjectByType<ScoreUIController>();
            }
        }

        void Start()
        {
            Debug.Log(ScorePersistence.GetHighScore(currentLevel));
        }
        private void OnEnable()
        {
            GameSignals.CountdownStarted += HandleCountdownStarted;
            GameSignals.RunEnded += HandleRunEnded;
        }

        private void OnDisable()
        {
            GameSignals.CountdownStarted -= HandleCountdownStarted;
            GameSignals.RunEnded -= HandleRunEnded;
        }

        public void RegisterObstaclePassed()
        {
            if (!IsRunActive)
                return;

            ObstaclesPassed++;
            Score += pointsPerObstacle;
            RefreshScoreUI();
        }

        public void RegisterNearMiss()
        {
            if (!IsRunActive)
                return;

            NearMissCount++;
            Score += nearMissBonus;
            RefreshScoreUI();

            //Debug.Log($"[ScoreManager] Near miss! +{nearMissBonus} | Score={Score} | NearMisses={NearMissCount}");
        }

        public void RegisterBeatBonus(BeatAccuracy accuracy)
        {
            if (!IsRunActive)
                return;

            int bonus = 0;

            switch (accuracy)
            {
                case BeatAccuracy.Good:
                    bonus = goodBeatBonus;
                    break;
                case BeatAccuracy.Perfect:
                    bonus = perfectBeatBonus;
                    break;
            }

            if (bonus <= 0)
                return;

            Score += bonus;
            RefreshScoreUI();
            //Debug.Log($"[ScoreManager] Beat bonus {accuracy}! +{bonus} | Score={Score}");
        }

        public int GetHighScore()
        {
            if (currentLevel == null || string.IsNullOrWhiteSpace(currentLevel.LevelId))
                return 0;

            return PlayerPrefs.GetInt(GetHighScoreKey(), 0);
        }

        public bool IsNewHighScore(int score)
        {
            return score > GetHighScore();
        }

        public void ResetHighScore()
        {
            if (currentLevel == null || string.IsNullOrWhiteSpace(currentLevel.LevelId))
                return;

            PlayerPrefs.DeleteKey(GetHighScoreKey());
            PlayerPrefs.Save();

            Debug.Log($"[ScoreManager] High score reset for level '{currentLevel.LevelId}'.");
        }

        private void HandleCountdownStarted()
        {
            ResetRunScore();
            IsRunActive = true;
        }

        private void HandleRunEnded()
        {
            IsRunActive = false;

            bool isNewHighScore = SaveHighScoreIfNeeded(Score);

            Debug.Log(
                $"[ScoreManager] Run ended. Final Score={Score} | " +
                $"HighScore={GetHighScore()} | " +
                $"NewHighScore={isNewHighScore} | " +
                $"ObstaclesPassed={ObstaclesPassed} | " +
                $"NearMisses={NearMissCount}");
        }

        private void ResetRunScore()
        {
            Score = 0;
            ObstaclesPassed = 0;
            NearMissCount = 0;
            RefreshScoreUI();

            //Debug.Log("[ScoreManager] Score reset for new run.");
        }

        private void RefreshScoreUI()
        {
            if (scoreUIController != null)
            {
                scoreUIController.SetScore(Score);
            }
        }

        private bool SaveHighScoreIfNeeded(int score)
        {
            if (currentLevel == null)
            {
                Debug.LogWarning("[ScoreManager] Cannot save high score because no LevelDefinition is assigned.", this);
                return false;
            }

            if (string.IsNullOrWhiteSpace(currentLevel.LevelId))
            {
                Debug.LogWarning("[ScoreManager] Cannot save high score because LevelDefinition.LevelId is empty.", this);
                return false;
            }

            int currentHighScore = GetHighScore();

            if (score <= currentHighScore)
                return false;

            PlayerPrefs.SetInt(GetHighScoreKey(), score);
            PlayerPrefs.Save();

            Debug.Log($"[ScoreManager] New high score for '{currentLevel.LevelId}': {score}");

            return true;
        }

        private string GetHighScoreKey()
        {
            return $"HIGHSCORE_{currentLevel.LevelId}";
        }
    }
}