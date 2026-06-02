using EchoRun.Core;
using EchoRun.Level;
using UnityEngine;

namespace EchoRun.Visuals
{
    [ExecuteAlways]
    public sealed class TrackMaterialScroller : MonoBehaviour
    {
        private static readonly int LineScrollId = Shader.PropertyToID("_LineScroll");
        private static readonly int HalfExtentsId = Shader.PropertyToID("_HalfExtents");

        [Header("References")]
        [SerializeField] private LevelSession levelSession;
        [SerializeField] private Material targetMaterial;

        [Header("Track Shape")]
        [SerializeField] private int gameplayLaneCount = 3;
        [SerializeField] private float laneWidth = 2.5f;
        [SerializeField] private float trackLength = 60f;
        [SerializeField] private float trackHalfHeight = 0.5f;

        private LevelDefinition _currentLevel;
        private float _scroll;
        private bool _isRunning;

        private void Awake()
        {
            if (levelSession == null)
                levelSession = FindFirstObjectByType<LevelSession>();

            ApplyTrackSettings();
            ApplyScroll();
        }

        private void Start()
        {
            if (levelSession != null)
                HandleLevelChanged(levelSession.CurrentLevel);
        }

        private void OnEnable()
        {
            if (levelSession != null)
                levelSession.LevelChanged += HandleLevelChanged;

            GameSignals.CountdownStarted += HandleCountdownStarted;
            GameSignals.ContinueCountdownStarted += HandleContinueCountdownStarted;
            GameSignals.GameplayStarted += HandleGameplayStarted;
            GameSignals.RunEnded += HandleRunEnded;
            GameSignals.RunCleanupRequested += HandleRunCleanupRequested;
        }

        private void OnDisable()
        {
            if (levelSession != null)
                levelSession.LevelChanged -= HandleLevelChanged;

            GameSignals.CountdownStarted -= HandleCountdownStarted;
            GameSignals.ContinueCountdownStarted -= HandleContinueCountdownStarted;
            GameSignals.GameplayStarted -= HandleGameplayStarted;
            GameSignals.RunEnded -= HandleRunEnded;
            GameSignals.RunCleanupRequested -= HandleRunCleanupRequested;
        }

        private void OnValidate()
        {
            gameplayLaneCount = Mathf.Max(1, gameplayLaneCount);
            laneWidth = Mathf.Max(0.01f, laneWidth);
            trackLength = Mathf.Max(0.01f, trackLength);
            trackHalfHeight = Mathf.Max(0.01f, trackHalfHeight);

            ApplyTrackSettings();
            ApplyScroll();
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
                ApplyTrackSettings();
                ApplyScroll();
                return;
            }

            if (!_isRunning || _currentLevel == null || targetMaterial == null)
                return;

            _scroll += _currentLevel.ScrollSpeed * Time.deltaTime;
            ApplyScroll();
        }

        private void HandleLevelChanged(LevelDefinition level)
        {
            _currentLevel = level;
            _scroll = 0f;

            ApplyTrackSettings();
            ApplyScroll();
        }

        private void HandleCountdownStarted()
        {
            _isRunning = true;
            _scroll = 0f;

            ApplyTrackSettings();
            ApplyScroll();
        }

        private void HandleContinueCountdownStarted(float continueSongTime)
        {
            if (_currentLevel == null)
                return;

            _scroll = continueSongTime * _currentLevel.ScrollSpeed;
            _isRunning = true;

            ApplyTrackSettings();
            ApplyScroll();
        }

        private void HandleGameplayStarted()
        {
            _isRunning = true;
        }

        private void HandleRunEnded()
        {
            _isRunning = false;
        }

        private void HandleRunCleanupRequested()
        {
            _isRunning = false;
        }

        private void ApplyTrackSettings()
        {
            if (targetMaterial == null)
                return;

            float halfWidth = gameplayLaneCount * laneWidth * 0.5f;
            float halfLength = trackLength * 0.5f;

            targetMaterial.SetVector(
                HalfExtentsId,
                new Vector4(halfWidth, trackHalfHeight, halfLength, 0f)
            );
        }

        private void ApplyScroll()
        {
            if (targetMaterial == null)
                return;

            targetMaterial.SetFloat(LineScrollId, _scroll);
        }
    }
}