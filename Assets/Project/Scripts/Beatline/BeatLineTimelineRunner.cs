using EchoRun.Core;
using UnityEngine;

namespace EchoRun.Level
{
    public sealed class BeatLineTimelineRunner : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private LevelSession levelSession;
        [SerializeField] private BeatLineSpawner beatLineSpawner;
        [SerializeField] private GameManager gameManager;

        [Header("Timing")]
        [SerializeField] private float beatTimeOffset = 0f;

        private LevelDefinition _currentLevel;
        private float _timelineTime;
        private int _nextBeatIndex;
        private bool _isRunning;
        private float _scrollSpeed;
        private float _spawnLeadTime;
        private float _totalLeadTime;

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
            if (!_isRunning || _currentLevel == null)
                return;

            _timelineTime += Time.deltaTime;

            var beatTimes = _currentLevel.BeatTimes;

            if (beatTimes == null || beatTimes.Count == 0)
                return;

            while (_nextBeatIndex < beatTimes.Count)
            {
                float beatTime = beatTimes[_nextBeatIndex] + beatTimeOffset;
                float spawnTime = beatTime - _spawnLeadTime;

                if (_timelineTime < spawnTime)
                    break;

                if (beatLineSpawner != null)
                    beatLineSpawner.Spawn(_scrollSpeed);

                _nextBeatIndex++;
            }
        }

        private void HandleLevelChanged(LevelDefinition level)
        {
            _currentLevel = level;
            RecalculateTimingData();
            _nextBeatIndex = 0;
            _timelineTime = 0f;
        }

        private void HandleCountdownStarted()
        {
            RecalculateTimingData();

            _timelineTime = -_totalLeadTime;
            _nextBeatIndex = 0;
            _isRunning = true;

            Debug.Log("Beat line timeline started.");
        }

        private void HandleContinueCountdownStarted(float continueSongTime)
        {
            RecalculateTimingData();

            float startTimelineTime = Mathf.Max(0f, continueSongTime - _totalLeadTime);

            _timelineTime = startTimelineTime;
            _nextBeatIndex = GetNextBeatIndexForTimelineTime(startTimelineTime);
            _isRunning = true;

            Debug.Log($"Beat line timeline continued. Timeline: {_timelineTime}, Next Beat: {_nextBeatIndex}");
        }

        private void HandleRunEnded()
        {
            _isRunning = false;
        }

        private int GetNextBeatIndexForTimelineTime(float timelineTime)
        {
            if (_currentLevel == null)
                return 0;

            var beatTimes = _currentLevel.BeatTimes;

            if (beatTimes == null || beatTimes.Count == 0)
                return 0;

            int index = 0;

            while (index < beatTimes.Count)
            {
                float beatTime = beatTimes[index] + beatTimeOffset;
                float spawnTime = beatTime - _spawnLeadTime;

                if (spawnTime >= timelineTime)
                    break;

                index++;
            }

            return index;
        }

        private void RecalculateTimingData()
        {
            if (_currentLevel == null || beatLineSpawner == null)
            {
                _scrollSpeed = 0f;
                _spawnLeadTime = 0f;
                _totalLeadTime = 0f;
                return;
            }

            _scrollSpeed = Mathf.Max(0.01f, _currentLevel.ScrollSpeed);
            _spawnLeadTime = beatLineSpawner.SpawnDistance / _scrollSpeed;

            float countdownDuration = 3f;
            float extraLeadTime = 0f;

            if (gameManager != null)
            {
                countdownDuration = gameManager.CountdownDuration;
                extraLeadTime = gameManager.ExtraLeadTime;
            }

            _totalLeadTime = countdownDuration + extraLeadTime;
        }
    }
}