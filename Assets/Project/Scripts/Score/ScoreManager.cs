using UnityEngine;
using EchoRun.Core;

namespace EchoRun.Gameplay
{
    public sealed class ScoreManager : MonoBehaviour
    {
        [Header("Tuning")]
        [SerializeField] private int pointsPerObstacle = 10;
        [SerializeField] private int nearMissBonus = 25;
        [SerializeField] private int goodBeatBonus = 5;
        [SerializeField] private int perfectBeatBonus = 10;

        public int Score { get; private set; }
        public int ObstaclesPassed { get; private set; }
        public int NearMissCount { get; private set; }
        public bool IsRunActive { get; private set; }

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

            //Debug.Log($"[ScoreManager] Passed obstacle. Score={Score} | ObstaclesPassed={ObstaclesPassed}");
        }

        public void RegisterNearMiss()
        {
            if (!IsRunActive)
                return;

            NearMissCount++;
            Score += nearMissBonus;

            Debug.Log($"[ScoreManager] Near miss! +{nearMissBonus} | Score={Score} | NearMisses={NearMissCount}");
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
            Debug.Log($"[ScoreManager] Beat bonus {accuracy}! +{bonus} | Score={Score}");
        }

        private void HandleCountdownStarted()
        {
            ResetRunScore();
            IsRunActive = true;
        }

        private void HandleRunEnded()
        {
            IsRunActive = false;
            Debug.Log($"[ScoreManager] Run ended. Final Score={Score} | ObstaclesPassed={ObstaclesPassed} | NearMisses={NearMissCount}");
        }

        private void ResetRunScore()
        {
            Score = 0;
            ObstaclesPassed = 0;
            NearMissCount = 0;
            Debug.Log("[ScoreManager] Score reset for new run.");
        }
    }
}