using EchoRun.Core;
using EchoRun.Gameplay;
using UnityEngine;
using EchoRun.UI;

namespace EchoRun.Player
{
    public sealed class PlayerBeatScorer : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RunnerMotor runnerMotor;
        [SerializeField] private BeatTimingScorer beatTimingScorer;
        [SerializeField] private ScoreManager scoreManager;

        [SerializeField] private BeatPopupPool popupPool;

        private void Awake()
        {
            if (runnerMotor == null)
            {
                runnerMotor = GetComponent<RunnerMotor>();
            }

            if (beatTimingScorer == null)
            {
                beatTimingScorer = FindFirstObjectByType<BeatTimingScorer>();
            }

            if (scoreManager == null)
            {
                scoreManager = FindFirstObjectByType<ScoreManager>();
            }
        }

        private void OnEnable()
        {
            GameSignals.LaneChanged += HandleLaneChanged;
            GameSignals.JumpPerformed += HandleJumpPerformed;
            GameSignals.SlideStarted += HandleSlideStarted;
            GameSignals.FastFallStarted += HandleFastFallStarted;
        }

        private void OnDisable()
        {
            GameSignals.LaneChanged -= HandleLaneChanged;
            GameSignals.JumpPerformed -= HandleJumpPerformed;
            GameSignals.SlideStarted -= HandleSlideStarted;
            GameSignals.FastFallStarted -= HandleFastFallStarted;
        }

        private void HandleLaneChanged(int newLane)
        {
            if (runnerMotor == null)
                return;

            TryAwardBeatBonus(runnerMotor.LastLaneChangeTime, $"Lane Change {newLane}");
        }

        private void HandleJumpPerformed()
        {
            if (runnerMotor == null)
                return;

            TryAwardBeatBonus(runnerMotor.LastJumpPerformedTime, "Jump");
        }

        private void HandleSlideStarted()
        {
            if (runnerMotor == null)
                return;

            TryAwardBeatBonus(runnerMotor.LastSlideStartedTime, "Slide");
        }

        private void HandleFastFallStarted()
        {
            if (runnerMotor == null)
                return;

            TryAwardBeatBonus(runnerMotor.LastFastFallTime, "Fast Fall");
        }

       private void TryAwardBeatBonus(float actionTime, string actionName)
        {
            if (beatTimingScorer == null || scoreManager == null)
                return;

            BeatAccuracy accuracy = beatTimingScorer.Evaluate(actionTime);
            int bonus = scoreManager.RegisterBeatBonus(accuracy);

            if (popupPool != null && bonus > 0)
            {
                switch (accuracy)
                {
                    case BeatAccuracy.Perfect:
                        popupPool.Show($"PERFECT +{bonus}", Color.yellow);
                        break;

                    case BeatAccuracy.Good:
                        popupPool.Show($"GOOD +{bonus}", Color.cyan);
                        break;
                }
            }

            // Debug.Log($"[Action] {actionName} = {accuracy} +{bonus}");
        }
    }
}