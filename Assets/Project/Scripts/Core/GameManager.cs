using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using EchoRun.Gameplay;

namespace EchoRun.Core
{
    public sealed class GameManager : MonoBehaviour
    {
        [SerializeField] private int countdownSeconds = 3;
        [SerializeField] private float extraLeadTime = 1f;

        [SerializeField] private ScoreManager scoreManager;
        public GameState CurrentState { get; private set; } = GameState.WaitingToStart;

        public float CountdownDuration => countdownSeconds;
        public float ExtraLeadTime => extraLeadTime;
        public float TotalLeadTime => countdownSeconds + extraLeadTime;

        private Coroutine _countdownRoutine;

        private void OnEnable()
        {
           // Debug.Log("[GameManager] OnEnable");
            GameSignals.TapToStartRequested += HandleTapToStartRequested;
            GameSignals.PlayerDied += HandlePlayerDied;
            GameSignals.RetryRequested += HandleRetryRequested;
        }

        private void OnDisable()
        {
            //Debug.Log("[GameManager] OnDisable");
            GameSignals.TapToStartRequested -= HandleTapToStartRequested;
            GameSignals.PlayerDied -= HandlePlayerDied;
            GameSignals.RetryRequested -= HandleRetryRequested;
        }

        private void HandleTapToStartRequested()
        {
            //Debug.Log($"[GameManager] TapToStartRequested received. CurrentState={CurrentState}");

            if (CurrentState != GameState.WaitingToStart)
                return;

            CurrentState = GameState.Countdown;
           // Debug.Log("[GameManager] Raising CountdownStarted");
            GameSignals.RaiseCountdownStarted();

            if (_countdownRoutine != null)
            {
                StopCoroutine(_countdownRoutine);
            }

            _countdownRoutine = StartCoroutine(CountdownRoutine());
        }

        private IEnumerator CountdownRoutine()
        {
           // Debug.Log($"[GameManager] CountdownRoutine started. extraLeadTime={extraLeadTime}, countdownSeconds={countdownSeconds}");

            if (extraLeadTime > 0f)
            {
                yield return new WaitForSeconds(extraLeadTime);
            }

            for (int i = countdownSeconds; i >= 1; i--)
            {
                //Debug.Log($"[GameManager] Raising CountdownTicked: {i}");
                GameSignals.RaiseCountdownTicked(i);
                yield return new WaitForSeconds(1f);
            }

           // Debug.Log("[GameManager] Raising CountdownGo");
            GameSignals.RaiseCountdownGo();

            CurrentState = GameState.Running;

            //Debug.Log("[GameManager] Raising GameplayStarted");
            GameSignals.RaiseGameplayStarted();

            _countdownRoutine = null;
        }

        private void HandlePlayerDied()
        {
            //Debug.Log($"[GameManager] PlayerDied received. CurrentState={CurrentState}");

            if (CurrentState != GameState.Running && CurrentState != GameState.Countdown)
                return;

            CurrentState = GameState.GameOver;

            if (_countdownRoutine != null)
            {
                StopCoroutine(_countdownRoutine);
                _countdownRoutine = null;
            }

           // Debug.Log("[GameManager] Raising RunEnded");
            GameSignals.RaiseRunEnded();
        }

        private void HandleRetryRequested()
        {
            //Debug.Log($"[GameManager] RetryRequested received. CurrentState={CurrentState}");

            if (CurrentState != GameState.GameOver)
                return;

            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}