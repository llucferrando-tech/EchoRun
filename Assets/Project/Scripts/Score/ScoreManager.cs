using UnityEngine;
using EchoRun.Core;
using EchoRun.Level;
using EchoRun.UI;

namespace EchoRun.Gameplay
{
    public sealed class ScoreManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private LevelSession levelSession;
        [SerializeField] private ScoreUIController scoreUIController;

        [Header("Tuning")]
        [SerializeField] private int pointsPerObstacle = 10;
        [SerializeField] private int nearMissBonus = 25;
        [SerializeField] private int goodBeatBonus = 5;
        [SerializeField] private int perfectBeatBonus = 10;

        private LevelDefinition _currentLevel;

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

            if (levelSession == null)
            {
                levelSession = FindFirstObjectByType<LevelSession>();
            }
        }

        private void Start()
        {
            if (levelSession != null)
            {
                HandleLevelChanged(levelSession.CurrentLevel);
            }
        }

        private void OnEnable()
        {
            GameSignals.CountdownStarted += HandleCountdownStarted;
            GameSignals.RunEnded += HandleRunEnded;

            if (levelSession != null)
            {
                levelSession.LevelChanged += HandleLevelChanged;
            }
        }

        private void OnDisable()
        {
            GameSignals.CountdownStarted -= HandleCountdownStarted;
            GameSignals.RunEnded -= HandleRunEnded;

            if (levelSession != null)
            {
                levelSession.LevelChanged -= HandleLevelChanged;
            }
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
        }

        public int RegisterBeatBonus(BeatAccuracy accuracy)
        {
            if (!IsRunActive)
                return 0;

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
                return 0;

            Score += bonus;
            RefreshScoreUI();

            return bonus;
        }

        public int GetHighScore()
        {
            return ScorePersistence.GetHighScore(_currentLevel);
        }

        public bool IsNewHighScore(int score)
        {
            return score > GetHighScore();
        }

        public void ResetHighScore()
        {
            ScorePersistence.ResetHighScore(_currentLevel);
        }

        private void HandleLevelChanged(LevelDefinition newLevel)
        {
            _currentLevel = newLevel;

            if (_currentLevel != null)
            {
                Debug.Log($"[ScoreManager] Current level set to '{_currentLevel.name}' | HighScore={GetHighScore()}");
            }
        }

        private void HandleCountdownStarted()
        {
            ResetRunScore();
            IsRunActive = true;
        }

        private void HandleRunEnded()
        {
            IsRunActive = false;

            int oldHighScore = GetHighScore();
            ScorePersistence.SetHighScore(_currentLevel, Score);
            int newHighScore = GetHighScore();

            Debug.Log(
                $"[ScoreManager] Run ended. Final Score={Score} | " +
                $"HighScore={newHighScore} | " +
                $"NewHighScore={newHighScore > oldHighScore} | " +
                $"ObstaclesPassed={ObstaclesPassed} | " +
                $"NearMisses={NearMissCount}");
        }

        private void ResetRunScore()
        {
            Score = 0;
            ObstaclesPassed = 0;
            NearMissCount = 0;
            RefreshScoreUI();
        }

        private void RefreshScoreUI()
        {
            if (scoreUIController != null)
            {
                scoreUIController.SetScore(Score);
            }
        }
    }
}