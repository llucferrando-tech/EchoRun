using System.Collections;
using UnityEngine;
using EchoRun.Gameplay;
using EchoRun.Level;

namespace EchoRun.Core
{
    public sealed class GameManager : MonoBehaviour
    {
        [SerializeField] private int countdownSeconds = 3;
        [SerializeField] private float extraLeadTime = 1f;

        [SerializeField] private ScoreManager scoreManager;
        [SerializeField] private LevelSession levelSession;

        public GameState CurrentState { get; private set; } = GameState.SongSelect;

        public float CountdownDuration => countdownSeconds;
        public float ExtraLeadTime => extraLeadTime;
        public float TotalLeadTime => countdownSeconds + extraLeadTime;

        private Coroutine _countdownRoutine;

        private void Awake()
        {
            if (levelSession == null)
            {
                levelSession = FindFirstObjectByType<LevelSession>();
            }
        }

        private void OnEnable()
        {
            GameSignals.LevelSelected += HandleLevelSelected;
            GameSignals.TapToStartRequested += HandleTapToStartRequested;
            GameSignals.PlayerDied += HandlePlayerDied;
            GameSignals.LevelCompleted += HandleLevelCompleted;
            GameSignals.RetryRequested += HandleRetryRequested;
            GameSignals.BackToSongSelectRequested += HandleBackToSongSelectRequested;
        }

        private void OnDisable()
        {
            GameSignals.LevelSelected -= HandleLevelSelected;
            GameSignals.TapToStartRequested -= HandleTapToStartRequested;
            GameSignals.PlayerDied -= HandlePlayerDied;
            GameSignals.LevelCompleted -= HandleLevelCompleted;
            GameSignals.RetryRequested -= HandleRetryRequested;
            GameSignals.BackToSongSelectRequested -= HandleBackToSongSelectRequested;
        }

        private void HandleLevelSelected(LevelDefinition selectedLevel)
        {
            if (selectedLevel == null)
                return;

            if (levelSession != null)
            {
                levelSession.SetLevel(selectedLevel);
            }

            CurrentState = GameState.WaitingToStart;
        }

        private void HandleTapToStartRequested()
        {
            if (CurrentState != GameState.WaitingToStart)
                return;

            StartCountdown();
        }

        private void StartCountdown()
        {
            CurrentState = GameState.Countdown;
            GameSignals.RaiseCountdownStarted();

            if (_countdownRoutine != null)
            {
                StopCoroutine(_countdownRoutine);
            }

            _countdownRoutine = StartCoroutine(CountdownRoutine());
        }

        private IEnumerator CountdownRoutine()
        {
            if (extraLeadTime > 0f)
            {
                yield return new WaitForSeconds(extraLeadTime);
            }

            for (int i = countdownSeconds; i >= 1; i--)
            {
                GameSignals.RaiseCountdownTicked(i);
                yield return new WaitForSeconds(1f);
            }

            GameSignals.RaiseCountdownGo();

            CurrentState = GameState.Running;
            GameSignals.RaiseGameplayStarted();

            _countdownRoutine = null;
        }

        private void HandlePlayerDied()
        {
            if (CurrentState != GameState.Running && CurrentState != GameState.Countdown)
                return;

            if (_countdownRoutine != null)
            {
                StopCoroutine(_countdownRoutine);
                _countdownRoutine = null;
            }

            CurrentState = GameState.Defeat;
            GameSignals.RaiseRunLost();
            GameSignals.RaiseRunEnded();
        }

        private void HandleLevelCompleted()
        {
            if (CurrentState != GameState.Running)
                return;

            CurrentState = GameState.Victory;
            GameSignals.RaiseRunWon();
            GameSignals.RaiseRunEnded();
        }

        private void HandleRetryRequested()
        {
            if (CurrentState != GameState.Defeat && CurrentState != GameState.Victory)
                return;

            if (levelSession == null || levelSession.CurrentLevel == null)
                return;

            StartCountdown();
        }

        private void HandleBackToSongSelectRequested()
        {
            if (CurrentState != GameState.Defeat && CurrentState != GameState.Victory)
                return;

            if (_countdownRoutine != null)
            {
                StopCoroutine(_countdownRoutine);
                _countdownRoutine = null;
            }

            CurrentState = GameState.SongSelect;
        }
    }
}