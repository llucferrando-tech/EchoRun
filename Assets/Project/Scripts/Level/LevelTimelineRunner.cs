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

        [Tooltip("Extra time after the final encounter before the level is considered complete.")]
        [SerializeField] private float levelCompletePadding = 1f;

        private LevelDefinition _currentLevel;
        private float _timelineTime;
        private int _nextEventIndex;
        private bool _isRunning;
        private bool _completionRaised;
        private float _scrollSpeed;
        private float _spawnLeadTime;
        private float _totalLeadTime;
        private float _levelEndTime;

        private void Awake()
        {
            if (levelSession == null)
                levelSession = FindFirstObjectByType<LevelSession>();

            RecalculateTimingData();
        }

        private void Start()
        {
            if (levelSession != null)
                HandleLevelChanged(levelSession.CurrentLevel);
        }

        private void OnValidate()
        {
            RecalculateTimingData();
        }

        private void OnEnable()
        {
            GameSignals.CountdownStarted += HandleCountdownStarted;
            GameSignals.ContinueCountdownStarted += HandleContinueCountdownStarted;
            GameSignals.RunEnded += HandleRunEnded;

            if (levelSession != null)
                levelSession.LevelChanged += HandleLevelChanged;
        }

        private void OnDisable()
        {
            GameSignals.CountdownStarted -= HandleCountdownStarted;
            GameSignals.ContinueCountdownStarted -= HandleContinueCountdownStarted;
            GameSignals.RunEnded -= HandleRunEnded;

            if (levelSession != null)
                levelSession.LevelChanged -= HandleLevelChanged;
        }

        private void Update()
        {
            if (!_isRunning)
                return;

            if (_currentLevel == null)
                return;

            _timelineTime += Time.deltaTime;

            var events = _currentLevel.Events;

            if (obstacleSpawner != null && events != null && events.Count > 0)
            {
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

            TryCompleteLevel(events);
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
            _completionRaised = false;
            _isRunning = true;
        }

        private void HandleContinueCountdownStarted(float continueSongTime)
        {
            RecalculateTimingData();

            float startTimelineTime = Mathf.Max(0f, continueSongTime - _totalLeadTime);

            _timelineTime = startTimelineTime;
            _nextEventIndex = GetNextEventIndexForTimelineTime(startTimelineTime);
            _completionRaised = false;
            _isRunning = true;

            Debug.Log($"Continue timeline countdown started. Timeline: {_timelineTime}, Next Event: {_nextEventIndex}");
        }

        private void HandleRunEnded()
        {
            _isRunning = false;
        }

        private int GetNextEventIndexForTimelineTime(float timelineTime)
        {
            if (_currentLevel == null)
                return 0;

            var events = _currentLevel.Events;

            if (events == null || events.Count == 0)
                return 0;

            int index = 0;

            while (index < events.Count)
            {
                LevelEventData eventData = events[index];
                float adjustedEncounterTime = eventData.time + encounterTimeOffset;
                float spawnTime = adjustedEncounterTime - _spawnLeadTime;

                if (spawnTime >= timelineTime)
                    break;

                index++;
            }

            return index;
        }

        private void TryCompleteLevel(System.Collections.Generic.IReadOnlyList<LevelEventData> events)
        {
            if (_completionRaised)
                return;

            if (_currentLevel == null)
                return;

            if (events == null || events.Count == 0)
            {
                if (_timelineTime >= 0f)
                {
                    _completionRaised = true;
                    _isRunning = false;
                    GameSignals.RaiseLevelCompleted();
                }

                return;
            }

            bool allEventsSpawned = _nextEventIndex >= events.Count;
            bool reachedEndTime = _timelineTime >= _levelEndTime;

            if (!allEventsSpawned || !reachedEndTime)
                return;

            _completionRaised = true;
            _isRunning = false;
            GameSignals.RaiseLevelCompleted();
        }

        private void RecalculateTimingData()
        {
            if (_currentLevel == null || obstacleSpawner == null)
            {
                _scrollSpeed = 0f;
                _spawnLeadTime = 0f;
                _totalLeadTime = 0f;
                _levelEndTime = 0f;
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

            var events = _currentLevel.Events;

            if (events == null || events.Count == 0)
            {
                _levelEndTime = 0f;
                return;
            }

            float lastEncounterTime = events[events.Count - 1].time + encounterTimeOffset;
            _levelEndTime = lastEncounterTime + levelCompletePadding;
        }
    }
}