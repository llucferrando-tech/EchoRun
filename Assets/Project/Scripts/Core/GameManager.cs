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

        public GameState CurrentState { get; private set; } = GameState.MainMenu;

        public float CountdownDuration => countdownSeconds;
        public float ExtraLeadTime => extraLeadTime;
        public float TotalLeadTime => countdownSeconds + extraLeadTime;

        private Coroutine _countdownRoutine;
        private GameState _stateBeforePause = GameState.Running;

        private bool _isContinueCountdown;
        private float _continueSongTime;

        private void Awake()
        {
            if (levelSession == null)
                levelSession = FindFirstObjectByType<LevelSession>();
        }

        private void OnEnable()
        {
            GameSignals.LevelSelected += HandleLevelSelected;
            GameSignals.TapToStartRequested += HandleTapToStartRequested;
            GameSignals.PlayerDied += HandlePlayerDied;
            GameSignals.LevelCompleted += HandleLevelCompleted;
            GameSignals.RetryRequested += HandleRetryRequested;
            GameSignals.BackToSongSelectRequested += HandleBackToSongSelectRequested;
            GameSignals.PauseRequested += HandlePauseRequested;
            GameSignals.ResumeRequested += HandleResumeRequested;
            GameSignals.ContinueCheckpointRequested += HandleContinueCheckpointRequested;
            GameSignals.MainMenuStartRequested += HandleMainMenuStartRequested;
        }

        private void OnDisable()
        {
            GameSignals.LevelSelected -= HandleLevelSelected;
            GameSignals.TapToStartRequested -= HandleTapToStartRequested;
            GameSignals.PlayerDied -= HandlePlayerDied;
            GameSignals.LevelCompleted -= HandleLevelCompleted;
            GameSignals.RetryRequested -= HandleRetryRequested;
            GameSignals.BackToSongSelectRequested -= HandleBackToSongSelectRequested;
            GameSignals.PauseRequested -= HandlePauseRequested;
            GameSignals.ResumeRequested -= HandleResumeRequested;
            GameSignals.ContinueCheckpointRequested -= HandleContinueCheckpointRequested;
            GameSignals.MainMenuStartRequested -= HandleMainMenuStartRequested;
        }

        private void HandleLevelSelected(LevelDefinition selectedLevel)
        {
            if (selectedLevel == null)
                return;

            if (levelSession != null)
                levelSession.SetLevel(selectedLevel);

            SetState(GameState.WaitingToStart);
            Time.timeScale = 1f;
        }

        private void HandleTapToStartRequested()
        {
            if (CurrentState != GameState.WaitingToStart)
                return;

            StartCountdown(false, 0f);
        }

        private void StartCountdown(bool isContinueCountdown, float continueSongTime)
        {
            Time.timeScale = 1f;

            _isContinueCountdown = isContinueCountdown;
            _continueSongTime = continueSongTime;

            CurrentState = GameState.Countdown;

            if (_countdownRoutine != null)
                StopCoroutine(_countdownRoutine);

            GameSignals.RaiseCountdownStarted();

            if (_isContinueCountdown)
                GameSignals.RaiseContinueCountdownStarted(_continueSongTime);

            _countdownRoutine = StartCoroutine(CountdownRoutine());
        }

        private IEnumerator CountdownRoutine()
        {
            if (extraLeadTime > 0f)
                yield return new WaitForSeconds(extraLeadTime);

            for (int i = countdownSeconds; i >= 1; i--)
            {
                GameSignals.RaiseCountdownTicked(i);
                yield return new WaitForSeconds(1f);
            }

            GameSignals.RaiseCountdownGo();
            Debug.Log("go");

            yield return new WaitForSeconds(0.5f);

            CurrentState = GameState.Running;

            if (_isContinueCountdown)
                GameSignals.RaiseContinueRunAtSongTimeRequested(_continueSongTime);

            GameSignals.RaiseGameplayStarted();

            _isContinueCountdown = false;
            _continueSongTime = 0f;
            _countdownRoutine = null;
        }

        private void HandlePauseRequested()
        {
            if (CurrentState != GameState.Running)
                return;

            _stateBeforePause = CurrentState;
            CurrentState = GameState.Paused;
            Time.timeScale = 0f;
        }

        private void HandleResumeRequested()
        {
            if (CurrentState != GameState.Paused)
                return;

            CurrentState = _stateBeforePause;
            Time.timeScale = 1f;
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

            _isContinueCountdown = false;
            _continueSongTime = 0f;

            Time.timeScale = 1f;
            CurrentState = GameState.Defeat;

            GameSignals.RaiseRunLost();
            GameSignals.RaiseRunEnded();
        }

        private void HandleLevelCompleted()
        {
            if (CurrentState != GameState.Running)
                return;

            Time.timeScale = 1f;
            CurrentState = GameState.Victory;

            GameSignals.RaiseRunWon();
            GameSignals.RaiseRunEnded();
        }

        private void HandleContinueCheckpointRequested(float continueSongTime)
        {
            if (CurrentState != GameState.Defeat)
                return;

            if (_countdownRoutine != null)
            {
                StopCoroutine(_countdownRoutine);
                _countdownRoutine = null;
            }

            GameSignals.RaiseRunCleanupRequested();

            StartCountdown(true, continueSongTime);

            Debug.Log($"Continue countdown started for song time: {continueSongTime}");
        }

        private void HandleRetryRequested()
        {
            if (CurrentState != GameState.Defeat &&
                CurrentState != GameState.Victory &&
                CurrentState != GameState.Paused)
                return;

            if (levelSession == null || levelSession.CurrentLevel == null)
                return;

            if (_countdownRoutine != null)
            {
                StopCoroutine(_countdownRoutine);
                _countdownRoutine = null;
            }

            _isContinueCountdown = false;
            _continueSongTime = 0f;

            if (CurrentState == GameState.Paused)
            {
                CurrentState = GameState.Defeat;
                GameSignals.RaiseRunEnded();
            }

            Time.timeScale = 1f;
            StartCountdown(false, 0f);
        }

        private void HandleBackToSongSelectRequested()
        {
            if (CurrentState != GameState.Defeat &&
                CurrentState != GameState.Victory &&
                CurrentState != GameState.Paused)
                return;

            if (_countdownRoutine != null)
            {
                StopCoroutine(_countdownRoutine);
                _countdownRoutine = null;
            }

            _isContinueCountdown = false;
            _continueSongTime = 0f;

            if (CurrentState == GameState.Paused)
            {
                CurrentState = GameState.Defeat;
                GameSignals.RaiseRunEnded();
            }

            Time.timeScale = 1f;
            CurrentState = GameState.SongSelect;
        }
        private void HandleMainMenuStartRequested()
        {
            if (CurrentState != GameState.MainMenu)
                return;

            Time.timeScale = 1f;
            SetState(GameState.SongSelect);
        }
        private void SetState(GameState newState)
        {
            if (CurrentState == newState)
                return;

            CurrentState = newState;
            GameSignals.RaiseStateChanged(CurrentState);

            Debug.Log($"Game state changed to: {CurrentState}");
        }
    }
}