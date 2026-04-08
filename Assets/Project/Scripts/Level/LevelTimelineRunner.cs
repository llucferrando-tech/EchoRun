using EchoRun.Core;
using UnityEngine;

namespace EchoRun.Level
{
    public sealed class LevelTimelineRunner : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private LevelSession levelSession;
        [SerializeField] private ObstacleSpawner obstacleSpawner;
        [SerializeField] private GameManager gameManager;

        [Header("Timing")]
        [Tooltip("Shifts all encounter times globally. Negative = earlier, Positive = later.")]
        [SerializeField] private float encounterTimeOffset = 0f;

        private LevelDefinition _currentLevel;
        private float _timelineTime;
        private int _nextEventIndex;
        private bool _isRunning;
        private float _spawnLeadTime;
        private float _scrollSpeed;
        private float _totalLeadTime;

        private void Awake()
        {
            if (levelSession == null)
            {
                levelSession = FindFirstObjectByType<LevelSession>();
            }

            RecalculateTimingData();
        }

        private void Start()
        {
            if (levelSession != null)
            {
                HandleLevelChanged(levelSession.CurrentLevel);
            }
        }

        private void OnValidate()
        {
            RecalculateTimingData();
        }

        private void OnEnable()
        {
            GameSignals.CountdownStarted += HandleCountdownStarted;
            GameSignals.RunEnded += HandleRunEnded;

            if (levelSession != null)
            {
                levelSession.LevelChanged += HandleLevelChanged;
            }
        }

        private void OnDisable()
        {
            GameSignals.CountdownStarted -= HandleCountdownStarted;
            GameSignals.RunEnded -= HandleRunEnded;

            if (levelSession != null)
            {
                levelSession.LevelChanged -= HandleLevelChanged;
            }
        }

        private void Update()
        {
            if (!_isRunning)
                return;

            if (_currentLevel == null)
                return;

            if (obstacleSpawner == null)
                return;

            var events = _currentLevel.Events;

            if (events == null || events.Count == 0)
                return;

            _timelineTime += Time.deltaTime;

            while (_nextEventIndex < events.Count)
            {
                LevelEventData eventData = events[_nextEventIndex];
                float adjustedEncounterTime = eventData.time + encounterTimeOffset;
                float spawnTime = adjustedEncounterTime - _spawnLeadTime;

                if (_timelineTime < spawnTime)
                    break;

                obstacleSpawner.Spawn(eventData, _scrollSpeed);
                _nextEventIndex++;
            }
        }

        private void HandleLevelChanged(LevelDefinition newLevel)
        {
            _currentLevel = newLevel;
            RecalculateTimingData();
        }

        private void HandleCountdownStarted()
        {
            RecalculateTimingData();

            _timelineTime = -_totalLeadTime;
            _nextEventIndex = 0;
            _isRunning = true;
        }

        private void HandleRunEnded()
        {
            _isRunning = false;
        }

        private void RecalculateTimingData()
        {
            if (_currentLevel == null || obstacleSpawner == null)
            {
                _scrollSpeed = 0f;
                _spawnLeadTime = 0f;
                _totalLeadTime = 0f;
                return;
            }

            _scrollSpeed = Mathf.Max(0.01f, _currentLevel.ScrollSpeed);
            _spawnLeadTime = obstacleSpawner.SpawnDistance / _scrollSpeed;

            float extraLeadTime = 0f;
            float countdownDuration = 3f;

            if (gameManager != null)
            {
                countdownDuration = gameManager.CountdownDuration;
                extraLeadTime = gameManager.ExtraLeadTime;
            }

            _totalLeadTime = countdownDuration + extraLeadTime;
        }
    }
}