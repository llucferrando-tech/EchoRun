using EchoRun.Core;
using EchoRun.Gameplay;
using EchoRun.Obstacles;
using UnityEngine;
using EchoRun.Level;

namespace EchoRun.Player
{
    public sealed class PlayerNearMissDetector : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RunnerMotor runnerMotor;
        [SerializeField] private ScoreManager scoreManager;

        [Header("Tuning")]
        [SerializeField] private float wallNearMissWindow = 0.20f;
        [SerializeField] private float gapNearMissWindow = 0.20f;
        [SerializeField] private float aerialNearMissWindow = 0.20f;
        [SerializeField] private float globalNearMissCooldown = 0.10f;

        private float _lastNearMissAwardTime = -999f;
        private bool _canEvaluate;

        private void Awake()
        {
            if (runnerMotor == null)
            {
                runnerMotor = GetComponent<RunnerMotor>();
            }

            if (scoreManager == null)
            {
                scoreManager = FindFirstObjectByType<ScoreManager>();
            }
        }

        private void OnEnable()
        {
            GameSignals.CountdownStarted += HandleCountdownStarted;
            GameSignals.GameplayStarted += HandleGameplayStarted;
            GameSignals.RunEnded += HandleRunEnded;

            GameSignals.LaneChanged += HandleLaneChanged;
            GameSignals.JumpPerformed += HandleJumpPerformed;
            GameSignals.SlideStarted += HandleSlideStarted;
        }

        private void OnDisable()
        {
            GameSignals.CountdownStarted -= HandleCountdownStarted;
            GameSignals.GameplayStarted -= HandleGameplayStarted;
            GameSignals.RunEnded -= HandleRunEnded;

            GameSignals.LaneChanged -= HandleLaneChanged;
            GameSignals.JumpPerformed -= HandleJumpPerformed;
            GameSignals.SlideStarted -= HandleSlideStarted;
        }

        private void HandleCountdownStarted()
        {
            _canEvaluate = false;
            _lastNearMissAwardTime = -999f;
        }

        private void HandleGameplayStarted()
        {
            _canEvaluate = true;
        }

        private void HandleRunEnded()
        {
            _canEvaluate = false;
        }

        private void HandleLaneChanged(int newLane)
        {
            if (!_canEvaluate || !CanAwardNearMiss())
                return;

            NearMissThreat[] threats = FindObjectsByType<NearMissThreat>(FindObjectsSortMode.None);

            for (int i = 0; i < threats.Length; i++)
            {
                NearMissThreat threat = threats[i];

                if (!IsValidThreat(threat, ObstacleType.Wall))
                    continue;

                if (!threat.IsNearPlayerLine())
                    continue;

                if (newLane == threat.Lane)
                    continue;

                float actionAge = GetSongTime() - runnerMotor.LastLaneChangeTime;
                if (actionAge > wallNearMissWindow)
                    continue;

                AwardNearMiss(threat, "Wall");
                break;
            }
        }

        private void HandleJumpPerformed()
        {
            if (!_canEvaluate || !CanAwardNearMiss())
                return;

            NearMissThreat[] threats = FindObjectsByType<NearMissThreat>(FindObjectsSortMode.None);

            for (int i = 0; i < threats.Length; i++)
            {
                NearMissThreat threat = threats[i];

                if (!IsValidThreat(threat, ObstacleType.Gap))
                    continue;

                if (threat.Lane != runnerMotor.CurrentLane)
                    continue;

                if (!threat.IsNearPlayerLine())
                    continue;

                float actionAge = GetSongTime() - runnerMotor.LastJumpPerformedTime;
                if (actionAge > gapNearMissWindow)
                    continue;

                AwardNearMiss(threat, "Gap");
                break;
            }
        }

        private void HandleSlideStarted()
        {
            if (!_canEvaluate || !CanAwardNearMiss())
                return;

            NearMissThreat[] threats = FindObjectsByType<NearMissThreat>(FindObjectsSortMode.None);

            for (int i = 0; i < threats.Length; i++)
            {
                NearMissThreat threat = threats[i];

                if (!IsValidThreat(threat, ObstacleType.Aerial))
                    continue;

                if (threat.Lane != runnerMotor.CurrentLane)
                    continue;

                if (!threat.IsNearPlayerLine())
                    continue;

                float actionAge = GetSongTime() - runnerMotor.LastSlideStartedTime;
                if (actionAge > aerialNearMissWindow)
                    continue;

                AwardNearMiss(threat, "Aerial");
                break;
            }
        }

        private bool IsValidThreat(NearMissThreat threat, ObstacleType expectedType)
        {
            if (threat == null)
                return false;

            if (!threat.IsInitialized || threat.IsConsumed)
                return false;

            if (threat.ObstacleType != expectedType)
                return false;

            return true;
        }

        private bool CanAwardNearMiss()
        {
            if (scoreManager == null || !scoreManager.IsRunActive)
                return false;

            return GetSongTime() - _lastNearMissAwardTime >= globalNearMissCooldown;
        }

        private void AwardNearMiss(NearMissThreat threat, string source)
        {
            threat.Consume();
            _lastNearMissAwardTime = GetSongTime();

            scoreManager.RegisterNearMiss();
            Debug.Log($"[PlayerNearMissDetector] {source} near miss.");
        }

        private float GetSongTime()
        {
            if (runnerMotor != null && runnerMotor.songTimeProvider != null)
                return runnerMotor.songTimeProvider.GetSongTime();

            return Time.time;
        }
    }
}