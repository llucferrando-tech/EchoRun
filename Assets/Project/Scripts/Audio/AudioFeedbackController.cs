using EchoRun.Core;
using MoreMountains.Feedbacks;
using UnityEngine;

namespace EchoRun.Audio
{
    public sealed class AudioFeedbackController : MonoBehaviour
    {
        [Header("Countdown")]
        [SerializeField] private MMF_Player countdownStartFeedback;

        [Header("Song")]
        [SerializeField] private MMF_Player levelSongStartFeedback;
        [SerializeField] private MMF_Player levelSongStopFeedback;

        [Header("Feedbacks")]
        [SerializeField] private MMF_Player runStartFeedback;
        [SerializeField] private MMF_Player runEndFeedback;
        [SerializeField] private MMF_Player jumpFeedback;
        [SerializeField] private MMF_Player laneChangeFeedback;
        [SerializeField] private MMF_Player deathFeedback;

        private void OnEnable()
        {
            GameSignals.CountdownStarted += HandleCountdownStarted;
            GameSignals.GameplayStarted += HandleGameplayStarted;
            GameSignals.RunEnded += HandleRunEnded;
            GameSignals.JumpPerformed += HandleJumpPerformed;
            GameSignals.LaneChanged += HandleLaneChanged;
            GameSignals.PlayerDied += HandlePlayerDied;
        }

        private void OnDisable()
        {
            GameSignals.CountdownStarted -= HandleCountdownStarted;
            GameSignals.GameplayStarted -= HandleGameplayStarted;
            GameSignals.RunEnded -= HandleRunEnded;
            GameSignals.JumpPerformed -= HandleJumpPerformed;
            GameSignals.LaneChanged -= HandleLaneChanged;
            GameSignals.PlayerDied -= HandlePlayerDied;
        }

        private void HandleCountdownStarted()
        {
            if (countdownStartFeedback != null)
                countdownStartFeedback.PlayFeedbacks();
        }

        private void HandleGameplayStarted()
        {
            if (levelSongStartFeedback != null)
                levelSongStartFeedback.PlayFeedbacks();

            if (runStartFeedback != null)
                runStartFeedback.PlayFeedbacks();

            //Debug.Log("Gameplay Started");
        }

        private void HandleRunEnded()
        {
            if (levelSongStopFeedback != null)
                levelSongStopFeedback.PlayFeedbacks();

            if (runEndFeedback != null)
                runEndFeedback.PlayFeedbacks();
        }

        private void HandleJumpPerformed()
        {
            if (jumpFeedback != null)
                jumpFeedback.PlayFeedbacks();
        }

        private void HandleLaneChanged(int _)
        {
            if (laneChangeFeedback != null)
                laneChangeFeedback.PlayFeedbacks();
        }

        private void HandlePlayerDied()
        {
            if (deathFeedback != null)
                deathFeedback.PlayFeedbacks();
        }
    }
}