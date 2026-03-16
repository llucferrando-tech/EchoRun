using UnityEngine;
using UnityEngine.SceneManagement;

namespace EchoRun.Core
{
    public sealed class GameManager : MonoBehaviour
    {
        public GameState CurrentState { get; private set; } = GameState.WaitingToStart;

        private void OnEnable()
        {
            GameSignals.TapToStartRequested += HandleTapToStartRequested;
            GameSignals.PlayerDied += HandlePlayerDied;
            GameSignals.RetryRequested += HandleRetryRequested;
        }

        private void OnDisable()
        {
            GameSignals.TapToStartRequested -= HandleTapToStartRequested;
            GameSignals.PlayerDied -= HandlePlayerDied;
            GameSignals.RetryRequested -= HandleRetryRequested;
        }

        private void HandleTapToStartRequested()
        {
            if (CurrentState != GameState.WaitingToStart)
                return;

            CurrentState = GameState.Running;
            GameSignals.RaiseRunStarted();
        }

        private void HandlePlayerDied()
        {
            if (CurrentState != GameState.Running)
                return;

            CurrentState = GameState.GameOver;
            GameSignals.RaiseRunEnded();
        }

        private void HandleRetryRequested()
        {
            if (CurrentState != GameState.GameOver)
                return;

            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}