using EchoRun.Core;
using UnityEngine;

namespace EchoRun.Level
{
    [RequireComponent(typeof(PooledBeatLine))]
    public sealed class BeatLineMover : MonoBehaviour
    {
        [SerializeField] private float despawnZ = -10f;

        private PooledBeatLine _pooledBeatLine;
        private float _moveSpeed;
        private bool _isRunning;

        private void Awake()
        {
            _pooledBeatLine = GetComponent<PooledBeatLine>();
        }

        private void OnEnable()
        {
            GameSignals.RunEnded += HandleRunEnded;
            GameSignals.RunCleanupRequested += HandleRunCleanupRequested;
            GameSignals.GameplayStarted += HandleGameplayStarted;
        }

        private void OnDisable()
        {
            GameSignals.RunEnded -= HandleRunEnded;
            GameSignals.RunCleanupRequested -= HandleRunCleanupRequested;
            GameSignals.GameplayStarted -= HandleGameplayStarted;
        }

        private void Update()
        {
            if (!_isRunning)
                return;

            transform.position += Vector3.back * _moveSpeed * Time.deltaTime;

            if (transform.position.z <= despawnZ)
                ReturnToPool();
        }

        public void Activate(float speed)
        {
            _moveSpeed = speed;
            _isRunning = true;
        }

        private void HandleGameplayStarted()
        {
            if (_moveSpeed > 0f)
                _isRunning = true;
        }

        private void HandleRunEnded()
        {
            _isRunning = false;
        }

        private void HandleRunCleanupRequested()
        {
            ReturnToPool();
        }

        private void ReturnToPool()
        {
            _isRunning = false;
            _pooledBeatLine.ReturnToPool();
        }
    }
}