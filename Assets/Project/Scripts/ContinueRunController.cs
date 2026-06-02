using EchoRun.Audio;
using EchoRun.Core;
using UnityEngine;

namespace EchoRun.Gameplay
{
    public sealed class ContinueRunController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SongTimeProvider songTimeProvider;
        [SerializeField] private Transform player;

        [Header("Continue Settings")]
        [SerializeField] private float rewindSeconds = 5f;
        [SerializeField] private Vector3 revivePosition = Vector3.zero;
        [SerializeField] private bool movePlayerToRevivePosition = true;

        private float _deathSongTime;

        private void Awake()
        {
            if (songTimeProvider == null)
                songTimeProvider = FindFirstObjectByType<SongTimeProvider>();
        }

        private void OnEnable()
        {
            GameSignals.PlayerDied += HandlePlayerDied;
            GameSignals.ContinueRunGranted += HandleContinueRunGranted;
            GameSignals.ContinueRunAtSongTimeRequested += HandleContinueRunAtSongTimeRequested;
        }

        private void OnDisable()
        {
            GameSignals.PlayerDied -= HandlePlayerDied;
            GameSignals.ContinueRunGranted -= HandleContinueRunGranted;
            GameSignals.ContinueRunAtSongTimeRequested -= HandleContinueRunAtSongTimeRequested;
        }

        private void HandlePlayerDied()
        {
            if (songTimeProvider == null)
            {
                Debug.LogError("ContinueRunController has no SongTimeProvider.");
                return;
            }

            _deathSongTime = songTimeProvider.GetSongTime();

            Debug.Log(
                $"Death song time saved: {_deathSongTime}. " +
                $"Provider: {songTimeProvider.name}. " +
                $"HasValidSource: {songTimeProvider.HasValidSource}. " +
                $"IsPlaying: {songTimeProvider.IsPlaying()}"
            );

            songTimeProvider.Pause();
        }

        private void HandleContinueRunGranted()
        {
            if (songTimeProvider == null)
                return;

            float continueTime = Mathf.Max(0f, _deathSongTime - rewindSeconds);

            songTimeProvider.Pause();
            songTimeProvider.SetSongTime(continueTime);

            if (movePlayerToRevivePosition && player != null)
                player.position = revivePosition;

            GameSignals.RaiseContinueCheckpointRequested(continueTime);

            Debug.Log($"Continue checkpoint requested at song time: {continueTime}");
        }

        private void HandleContinueRunAtSongTimeRequested(float songTime)
        {
            if (songTimeProvider == null)
                return;

            songTimeProvider.SetSongTime(songTime);
            songTimeProvider.Play();

            Debug.Log($"Continue song playback started at: {songTime}");
        }
    }
}