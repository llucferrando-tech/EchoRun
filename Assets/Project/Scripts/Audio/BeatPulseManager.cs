using System;
using UnityEngine;
using EchoRun.Audio;
using EchoRun.Core;

namespace EchoRun.Level
{
    public sealed class BeatPulseManager : MonoBehaviour
    {
        public static event Action BeatTriggered;

        [SerializeField] private LevelSession levelSession;
        [SerializeField] private SongTimeProvider songTimeProvider;
        [SerializeField] private float lookAhead = 0.02f;

        private LevelDefinition _currentLevel;
        private int _nextBeatIndex;
        private bool _running;

        private void Awake()
        {
            if (levelSession == null)
                levelSession = FindFirstObjectByType<LevelSession>();
        }

        private void Start()
        {
            if (levelSession != null)
                HandleLevelChanged(levelSession.CurrentLevel);
        }

        private void OnEnable()
        {
            GameSignals.CountdownStarted += HandleCountdownStarted;
            GameSignals.RunEnded += HandleRunEnded;
            GameSignals.ContinueRunAtSongTimeRequested += HandleContinueRunAtSongTimeRequested;

            if (levelSession != null)
                levelSession.LevelChanged += HandleLevelChanged;
        }

        private void OnDisable()
        {
            GameSignals.CountdownStarted -= HandleCountdownStarted;
            GameSignals.RunEnded -= HandleRunEnded;
            GameSignals.ContinueRunAtSongTimeRequested -= HandleContinueRunAtSongTimeRequested;

            if (levelSession != null)
                levelSession.LevelChanged -= HandleLevelChanged;
        }

        private void Update()
        {
            if (!_running || _currentLevel == null || songTimeProvider == null)
                return;

            var beatTimes = _currentLevel.BeatTimes;

            if (beatTimes == null || beatTimes.Count == 0)
                return;

            float songTime = songTimeProvider.GetSongTime();

            while (_nextBeatIndex < beatTimes.Count &&
                   songTime + lookAhead >= beatTimes[_nextBeatIndex])
            {
                BeatTriggered?.Invoke();
                _nextBeatIndex++;
            }
        }

        private void HandleLevelChanged(LevelDefinition newLevel)
        {
            _currentLevel = newLevel;
            _nextBeatIndex = 0;
        }

        private void HandleCountdownStarted()
        {
            _nextBeatIndex = 0;
            _running = true;
        }

        private void HandleRunEnded()
        {
            _running = false;
        }

        private void HandleContinueRunAtSongTimeRequested(float songTime)
        {
            SeekToSongTime(songTime);
            _running = true;
        }

        private void SeekToSongTime(float songTime)
        {
            _nextBeatIndex = 0;

            if (_currentLevel == null)
                return;

            var beatTimes = _currentLevel.BeatTimes;

            if (beatTimes == null || beatTimes.Count == 0)
                return;

            while (_nextBeatIndex < beatTimes.Count &&
                   beatTimes[_nextBeatIndex] < songTime)
            {
                _nextBeatIndex++;
            }
        }
    }
}