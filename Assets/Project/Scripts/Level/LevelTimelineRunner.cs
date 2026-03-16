using EchoRun.Core;
using UnityEngine;

namespace EchoRun.Level
{
    public sealed class LevelTimelineRunner : MonoBehaviour
    {
        [SerializeField] private LevelDefinition levelDefinition;
        [SerializeField] private ObstacleSpawner obstacleSpawner;

        private float _elapsedTime;
        private int _nextEventIndex;
        private bool _isRunning;
        private float _scrollSpeed;

        private void OnEnable()
        {
            GameSignals.RunStarted += HandleRunStarted;
            GameSignals.RunEnded += HandleRunEnded;
        }

        private void OnDisable()
        {
            GameSignals.RunStarted -= HandleRunStarted;
            GameSignals.RunEnded -= HandleRunEnded;
        }

        private void Update()
        {
            if (!_isRunning || levelDefinition == null || obstacleSpawner == null)
                return;

            var events = levelDefinition.Events;

            if (events == null || events.Count == 0)
                return;

            _elapsedTime += Time.deltaTime;

            while (_nextEventIndex < events.Count && _elapsedTime >= events[_nextEventIndex].time)
            {
                obstacleSpawner.Spawn(events[_nextEventIndex], levelDefinition.ScrollSpeed);
                _nextEventIndex++;
            }
        }

        private void HandleRunStarted()
        {
            _elapsedTime = 0f;
            _nextEventIndex = 0;
            _isRunning = true;
            _scrollSpeed = levelDefinition.ScrollSpeed;
        }

        private void HandleRunEnded()
        {
            _isRunning = false;
        }
    }
}