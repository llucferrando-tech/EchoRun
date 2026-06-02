using EchoRun.Audio;
using EchoRun.Core;
using UnityEngine;

namespace EchoRun.Level
{
    public sealed class LevelSongController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private LevelSession levelSession;
        [SerializeField] private SongTimeProvider songTimeProvider;
        [SerializeField] private AudioSource musicSource;

        private bool _isContinueCountdown;

        private void Awake()
        {
            if (levelSession == null)
                levelSession = FindFirstObjectByType<LevelSession>();

            if (songTimeProvider == null)
                songTimeProvider = FindFirstObjectByType<SongTimeProvider>();
        }

        private void Start()
        {
            if (levelSession != null)
                ApplyLevel(levelSession.CurrentLevel);
        }

        private void OnEnable()
        {
            if (levelSession != null)
                levelSession.LevelChanged += ApplyLevel;

            GameSignals.CountdownStarted += HandleCountdownStarted;
            GameSignals.ContinueCountdownStarted += HandleContinueCountdownStarted;
            GameSignals.GameplayStarted += HandleGameplayStarted;
            GameSignals.RunEnded += HandleRunEnded;
            GameSignals.ContinueRunAtSongTimeRequested += HandleContinueRunAtSongTimeRequested;
        }

        private void OnDisable()
        {
            if (levelSession != null)
                levelSession.LevelChanged -= ApplyLevel;

            GameSignals.CountdownStarted -= HandleCountdownStarted;
            GameSignals.ContinueCountdownStarted -= HandleContinueCountdownStarted;
            GameSignals.GameplayStarted -= HandleGameplayStarted;
            GameSignals.RunEnded -= HandleRunEnded;
            GameSignals.ContinueRunAtSongTimeRequested -= HandleContinueRunAtSongTimeRequested;
        }

        private void ApplyLevel(LevelDefinition level)
        {
            if (musicSource == null)
                return;

            musicSource.Stop();
            musicSource.clip = level != null ? level.SongClip : null;
            musicSource.time = 0f;

            _isContinueCountdown = false;

            Debug.Log($"Music clip set to: {(musicSource.clip != null ? musicSource.clip.name : "NULL")}");
        }

        private void HandleCountdownStarted()
        {
            if (songTimeProvider == null)
                return;

            songTimeProvider.Stop();

            if (!_isContinueCountdown)
                songTimeProvider.SetSongTime(0f);
        }

        private void HandleContinueCountdownStarted(float continueSongTime)
        {
            _isContinueCountdown = true;

            if (songTimeProvider == null)
                return;

            songTimeProvider.Stop();

            Debug.Log($"Continue countdown armed for song time: {continueSongTime}");
        }

        private void HandleGameplayStarted()
        {
            if (songTimeProvider == null)
                return;

            if (_isContinueCountdown)
            {
                _isContinueCountdown = false;
                Debug.Log("GameplayStarted ignored by music controller because continue already started music.");
                return;
            }

            songTimeProvider.SetSongTime(0f);
            songTimeProvider.Play();

            Debug.Log("Normal music started at 0.");
        }

        private void HandleRunEnded()
        {
            if (songTimeProvider == null)
                return;

            songTimeProvider.Pause();
        }

        private void HandleContinueRunAtSongTimeRequested(float songTime)
        {
            if (songTimeProvider == null)
                return;

            songTimeProvider.SetSongTime(songTime);
            songTimeProvider.Play();

            Debug.Log($"Continue music started at song time: {songTime}");
        }
    }
}