using UnityEngine;

namespace EchoRun.Level
{
    public sealed class DurationObstacleVisual : MonoBehaviour, IObstacleInitializable
    {
        [Header("References")]
        [SerializeField] private Transform visualRoot;
        [SerializeField] private BoxCollider obstacleCollider;

        [Header("Length")]
        [SerializeField] private float baseLength = 1f;
        [SerializeField] private float lengthPerSecond = 6f;

        [Header("Debug")]
        [SerializeField] private bool drawGizmos = true;

        private Vector3 _initialVisualScale;
        private Vector3 _initialVisualLocalPosition;
        private Vector3 _initialColliderCenter;
        private Vector3 _initialColliderSize;

        private float _currentTargetLength;

        private void Awake()
        {
            if (visualRoot == null)
            {
                visualRoot = transform;
            }

            _initialVisualScale = visualRoot.localScale;
            _initialVisualLocalPosition = visualRoot.localPosition;

            if (obstacleCollider != null)
            {
                _initialColliderCenter = obstacleCollider.center;
                _initialColliderSize = obstacleCollider.size;
            }

            _currentTargetLength = baseLength;
        }

        public void Initialize(LevelEventData eventData)
        {
            _currentTargetLength = Mathf.Max(baseLength, baseLength + eventData.duration * lengthPerSecond);

            float lengthFactor = _currentTargetLength / Mathf.Max(0.0001f, baseLength);
            float extraLength = _currentTargetLength - baseLength;

            // Scale visual proportionally, not by raw targetLength
            Vector3 visualScale = _initialVisualScale;
            visualScale.z = _initialVisualScale.z * lengthFactor;
            visualRoot.localScale = visualScale;

            // Offset the visual backward so the FRONT edge stays fixed
            Vector3 visualPosition = _initialVisualLocalPosition;
            visualPosition.z = _initialVisualLocalPosition.z + (extraLength * 0.5f);
            visualRoot.localPosition = visualPosition;

            if (obstacleCollider != null)
            {
                Vector3 colliderSize = _initialColliderSize;
                colliderSize.z = _initialColliderSize.z * lengthFactor;
                obstacleCollider.size = colliderSize;

                Vector3 colliderCenter = _initialColliderCenter;
                colliderCenter.z = _initialColliderCenter.z + (extraLength * 0.5f);
                obstacleCollider.center = colliderCenter;
            }

            Debug.Log(
                $"[DurationObstacleShape] duration={eventData.duration:F2}, " +
                $"targetLength={_currentTargetLength:F2}, factor={lengthFactor:F2}, extraLength={extraLength:F2}",
                this);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!drawGizmos)
                return;

            Gizmos.color = Color.white;
            Gizmos.DrawSphere(transform.position, 0.15f); // root pivot

            if (visualRoot != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(visualRoot.position, 0.12f);
            }

            if (obstacleCollider != null)
            {
                Matrix4x4 oldMatrix = Gizmos.matrix;
                Gizmos.matrix = obstacleCollider.transform.localToWorldMatrix;

                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(obstacleCollider.center, obstacleCollider.size);

                // Front face center
                Vector3 frontCenter = obstacleCollider.center + Vector3.forward * (obstacleCollider.size.z * 0.5f);
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(frontCenter, 0.08f);

                // Back face center
                Vector3 backCenter = obstacleCollider.center + Vector3.back * (obstacleCollider.size.z * 0.5f);
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(backCenter, 0.08f);

                Gizmos.matrix = oldMatrix;
            }
        }
#endif
    }
}