using EchoRun.Level;
using EchoRun.Player;
using UnityEngine;

namespace EchoRun.Obstacles
{
    public sealed class NearMissThreat : MonoBehaviour, IObstacleInitializable
    {
        [Header("Tuning")]
        [SerializeField] private float playerLineTolerance = 0.45f;

        private LevelEventData _eventData;
        private bool _isInitialized;
        private bool _isConsumed;
        private Transform _playerTransform;

        public ObstacleType ObstacleType => _eventData.obstacleType;
        public int Lane => _eventData.lane;
        public bool IsConsumed => _isConsumed;
        public bool IsInitialized => _isInitialized;

        public void Initialize(LevelEventData eventData)
        {
            _eventData = eventData;
            _isInitialized = true;
            _isConsumed = false;

            if (_playerTransform == null)
            {
                GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
                if (playerObject != null)
                {
                    _playerTransform = playerObject.transform;
                }
            }
        }

        public bool IsNearPlayerLine()
        {
            if (!_isInitialized || _isConsumed || _playerTransform == null)
                return false;

            float zDelta = Mathf.Abs(transform.position.z - _playerTransform.position.z);
            return zDelta <= playerLineTolerance;
        }

        public void Consume()
        {
            _isConsumed = true;
        }

        private void OnDisable()
        {
            _isInitialized = false;
            _isConsumed = false;
        }
    }
}