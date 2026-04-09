using UnityEngine;
using EchoRun.Core;
using EchoRun.Level;

namespace EchoRun.UI
{
    public sealed class OverlayStateController : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;

        [Header("Overlays")]
        [SerializeField] private GameObject songSelectOverlay;
        [SerializeField] private GameObject tapToStartOverlay;
        [SerializeField] private GameObject countdownOverlay;
        [SerializeField] private GameObject ingameOverlay;
        [SerializeField] private GameObject gameOverOverlay;

        private void Awake()
        {
            if (gameManager == null)
            {
                gameManager = FindFirstObjectByType<GameManager>();
            }
        }

        private void OnEnable()
        {
            GameSignals.LevelSelected += HandleLevelSelected;
            GameSignals.CountdownStarted += HandleStateChange;
            GameSignals.GameplayStarted += HandleStateChange;
            GameSignals.RunWon += HandleStateChange;
            GameSignals.RunLost += HandleStateChange;
            GameSignals.RunEnded += HandleStateChange;
            GameSignals.RetryRequested += HandleStateChange;
            GameSignals.BackToSongSelectRequested += HandleStateChange;
        }

        private void OnDisable()
        {
            GameSignals.LevelSelected -= HandleLevelSelected;
            GameSignals.CountdownStarted -= HandleStateChange;
            GameSignals.GameplayStarted -= HandleStateChange;
            GameSignals.RunWon -= HandleStateChange;
            GameSignals.RunLost -= HandleStateChange;
            GameSignals.RunEnded -= HandleStateChange;
            GameSignals.RetryRequested -= HandleStateChange;
            GameSignals.BackToSongSelectRequested -= HandleStateChange;
        }

        private void Start()
        {
            Refresh();
        }

        private void HandleStateChange()
        {
            Refresh();
        }

        private void HandleLevelSelected(LevelDefinition _)
        {
            Refresh();
        }

        private void Refresh()
        {
            if (gameManager == null)
                return;

            GameState state = gameManager.CurrentState;

            if (songSelectOverlay != null)
                songSelectOverlay.SetActive(state == GameState.SongSelect);

            if (tapToStartOverlay != null)
                tapToStartOverlay.SetActive(state == GameState.WaitingToStart);

            if (countdownOverlay != null)
                countdownOverlay.SetActive(state == GameState.Countdown);

            if (ingameOverlay != null)
                ingameOverlay.SetActive(state == GameState.Running);

            if (gameOverOverlay != null)
                gameOverOverlay.SetActive(
                    state == GameState.Victory ||
                    state == GameState.Defeat);
        }
    }
}