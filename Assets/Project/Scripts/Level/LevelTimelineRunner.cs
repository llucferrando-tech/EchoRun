using EchoRun.Core;
using UnityEngine;

namespace EchoRun.Level
{
    public sealed class LevelTimelineRunner : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private LevelDefinition levelDefinition;
        [SerializeField] private ObstacleSpawner obstacleSpawner;
        [SerializeField] private GameManager gameManager;

        [Header("Timing")]
        [Tooltip("Shifts all encounter times globally. Negative = earlier, Positive = later.")]
        [SerializeField] private float encounterTimeOffset = 0f;

        private float _timelineTime;
        private int _nextEventIndex;
        private bool _isRunning;
        private float _spawnLeadTime;
        private float _scrollSpeed;
        private float _totalLeadTime;

        private void Awake()
        {
            Debug.Log("[LevelTimelineRunner] Awake");
            RecalculateTimingData();
        }

        private void OnValidate()
        {
            RecalculateTimingData();
        }

        private void OnEnable()
        {
            Debug.Log("[LevelTimelineRunner] OnEnable");
            GameSignals.CountdownStarted += HandleCountdownStarted;
            GameSignals.RunEnded += HandleRunEnded;
        }

        private void OnDisable()
        {
            Debug.Log("[LevelTimelineRunner] OnDisable");
            GameSignals.CountdownStarted -= HandleCountdownStarted;
            GameSignals.RunEnded -= HandleRunEnded;
        }

        private void Update()
        {
            if (!_isRunning)
                return;

            if (levelDefinition == null)
            {
                Debug.LogWarning("[LevelTimelineRunner] levelDefinition is null");
                return;
            }

            if (obstacleSpawner == null)
            {
                Debug.LogWarning("[LevelTimelineRunner] obstacleSpawner is null");
                return;
            }

            var events = levelDefinition.Events;

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

                Debug.Log(
                    $"[LevelTimelineRunner] Spawning event index={_nextEventIndex}, " +
                    $"rawEncounterTime={eventData.time:F2}, adjustedEncounterTime={adjustedEncounterTime:F2}, " +
                    $"spawnTime={spawnTime:F2}, timelineTime={_timelineTime:F2}");

                obstacleSpawner.Spawn(eventData, _scrollSpeed);
                _nextEventIndex++;
            }
        }

        private void HandleCountdownStarted()
        {
            Debug.Log("[LevelTimelineRunner] CountdownStarted received");
            RecalculateTimingData();

            _timelineTime = -_totalLeadTime;
            _nextEventIndex = 0;
            _isRunning = true;

            Debug.Log(
                $"[LevelTimelineRunner] Timeline started. " +
                $"timelineTime={_timelineTime:F2}, spawnLeadTime={_spawnLeadTime:F2}, " +
                $"scrollSpeed={_scrollSpeed:F2}, totalLeadTime={_totalLeadTime:F2}, " +
                $"encounterTimeOffset={encounterTimeOffset:F2}");
        }

        private void HandleRunEnded()
        {
            Debug.Log("[LevelTimelineRunner] RunEnded received");
            _isRunning = false;
        }

        private void RecalculateTimingData()
        {
            if (levelDefinition == null || obstacleSpawner == null)
            {
                _scrollSpeed = 0f;
                _spawnLeadTime = 0f;
                _totalLeadTime = 0f;
                return;
            }

            _scrollSpeed = Mathf.Max(0.01f, levelDefinition.ScrollSpeed);
            _spawnLeadTime = obstacleSpawner.SpawnDistance / _scrollSpeed;

            float extraLeadTime = 0f;
            float countdownDuration = 3f;

            if (gameManager != null)
            {
                countdownDuration = gameManager.CountdownDuration;
                extraLeadTime = gameManager.ExtraLeadTime;
            }

            _totalLeadTime = countdownDuration + extraLeadTime;

            Debug.Log(
                $"[LevelTimelineRunner] RecalculateTimingData -> " +
                $"scrollSpeed={_scrollSpeed:F2}, spawnDistance={obstacleSpawner.SpawnDistance:F2}, " +
                $"spawnLeadTime={_spawnLeadTime:F2}, countdownDuration={countdownDuration:F2}, " +
                $"extraLeadTime={extraLeadTime:F2}, totalLeadTime={_totalLeadTime:F2}, " +
                $"encounterTimeOffset={encounterTimeOffset:F2}");
        }
    }
}