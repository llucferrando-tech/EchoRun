using EchoRun.Core;
using UnityEngine;

namespace EchoRun.Obstacles
{
    public sealed class MovingObstacle : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 8f;
        [SerializeField] private float despawnZ = -10f;

        private bool _isRunning;

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

        private void Start()
        {
            _isRunning = true;
        }

        private void Update()
        {
            if (!_isRunning)
                return;

            transform.position += Vector3.back * moveSpeed * Time.deltaTime;

            if (transform.position.z <= despawnZ)
            {
                Destroy(gameObject);
            }
        }

        private void HandleRunStarted()
        {
            _isRunning = true;
        }

        private void HandleRunEnded()
        {
            _isRunning = false;
        }
    }
}