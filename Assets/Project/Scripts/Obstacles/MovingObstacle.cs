using EchoRun.Core;
using EchoRun.Level;
using UnityEngine;

namespace EchoRun.Obstacles
{
    public sealed class MovingObstacle : MonoBehaviour
    {
        [SerializeField] private float despawnZ = -10f;

        private float _moveSpeed;
        private bool _isRunning;
        private PooledObstacle _pooledObstacle;

        private void Awake()
        {
            _pooledObstacle = GetComponent<PooledObstacle>();
        }

        private void OnEnable()
        {
            GameSignals.RunEnded += HandleRunEnded;
        }

        private void OnDisable()
        {
            GameSignals.RunEnded -= HandleRunEnded;
        }

        private void Update()
        {
            if (!_isRunning)
                return;

            transform.position += Vector3.back * _moveSpeed * Time.deltaTime;

            if (transform.position.z <= despawnZ)
            {
                ReturnToPool();
            }
        }

        public void Activate(float speed)
        {
            _moveSpeed = speed;
            _isRunning = true;
        }

        private void HandleRunEnded()
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