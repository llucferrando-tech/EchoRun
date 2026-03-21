using System;
using UnityEngine;
using EchoRun.Audio;
using EchoRun.Core;

namespace EchoRun.Level
{
    public sealed class BeatPulseManager : MonoBehaviour
    {
        public static event Action BeatTriggered;

        [SerializeField] private LevelDefinition levelDefinition;
        [SerializeField] private SongTimeProvider songTimeProvider;
        [SerializeField] private float lookAhead = 0.02f;

        private int _nextBeatIndex;
        private bool _running;

        private void OnEnable()
        {
            GameSignals.CountdownStarted += HandleCountdownStarted;
            GameSignals.RunEnded += HandleRunEnded;
        }

        private void OnDisable()
        {
            GameSignals.CountdownStarted -= HandleCountdownStarted;
            GameSignals.RunEnded -= HandleRunEnded;
        }

        private void Update()
        {
            if (!_running || levelDefinition == null || songTimeProvider == null)
                return;

            var beatTimes = levelDefinition.BeatTimes;
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

        private void HandleCountdownStarted()
        {
            _nextBeatIndex = 0;
            _running = true;
        }

        private void HandleRunEnded()
        {
            _running = false;
        }
    }
}