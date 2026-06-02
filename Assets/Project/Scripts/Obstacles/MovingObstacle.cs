using EchoRun.Core;
using UnityEngine;
using EchoRun.Level;
using EchoRun.Gameplay;


namespace EchoRun.Obstacles
{
    public sealed class MovingObstacle : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float despawnZ = -10f;

        [Header("Scoring")]
        [SerializeField] private Transform playerTransform;

        private float _moveSpeed;
        private bool _isRunning;
        private bool _hasScored;
        private PooledObstacle _pooledObstacle;
        private ScoreManager _scoreManager;

        private void Awake()
        {
            _pooledObstacle = GetComponent<PooledObstacle>();

            if (_scoreManager == null)
                _scoreManager = FindFirstObjectByType<ScoreManager>();

            FindPlayerIfNeeded();
        }

        private void OnEnable()
        {
            GameSignals.RunCleanupRequested += HandleRunCleanupRequested;
            GameSignals.GameplayStarted += HandleGameplayStarted;
            GameSignals.RunEnded += HandleRunEnded;

            _hasScored = false;
        }

        private void OnDisable()
        {
            GameSignals.RunCleanupRequested -= HandleRunCleanupRequested;
            GameSignals.GameplayStarted -= HandleGameplayStarted;
            GameSignals.RunEnded -= HandleRunEnded;
        }

        private void Update()
        {
            if (!_isRunning)
                return;

            transform.position += Vector3.back * _moveSpeed * Time.deltaTime;

            TryRegisterPassedScore();

            if (transform.position.z <= despawnZ)
                ReturnToPool();
        }

        public void Activate(float speed)
        {
            _moveSpeed = speed;
            _isRunning = true;
            _hasScored = false;

            if (_scoreManager == null)
                _scoreManager = FindFirstObjectByType<ScoreManager>();

            FindPlayerIfNeeded();
        }

        private void FindPlayerIfNeeded()
        {
            if (playerTransform != null)
                return;

            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
                playerTransform = playerObject.transform;
        }

        private void TryRegisterPassedScore()
        {
            if (_hasScored)
                return;

            if (_scoreManager == null)
                return;

            if (playerTransform == null)
                return;

            if (transform.position.z < playerTransform.position.z)
            {
                _hasScored = true;
                _scoreManager.RegisterObstaclePassed();
            }
        }

        private void HandleRunEnded()
        {
            _isRunning = false;
        }

        private void HandleGameplayStarted()
        {
            if (_moveSpeed > 0f)
                _isRunning = true;
        }

        private void HandleRunCleanupRequested()
        {
            _isRunning = false;
            ReturnToPool();
        }

        private void ReturnToPool()
        {
            _isRunning = false;

            if (_pooledObstacle != null)
            {
                _pooledObstacle.ReturnToPool();
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}