using UnityEngine;

namespace EchoRun.Level
{
    public sealed class DurationObstacleVisual : MonoBehaviour, IObstacleInitializable
    {
        [Header("Scaling")]
        [SerializeField] private Transform visualRoot;
        [SerializeField] private float minLength = 1f;
        [SerializeField] private float lengthPerSecond = 6f;

        private Vector3 _initialScale;

        private void Awake()
        {
            if (visualRoot == null)
            {
                visualRoot = transform;
            }

            _initialScale = visualRoot.localScale;
        }

        public void Initialize(LevelEventData eventData)
        {
            float targetLength = Mathf.Max(minLength, minLength + eventData.duration * lengthPerSecond);

            Vector3 scale = _initialScale;
            scale.z = targetLength;
            visualRoot.localScale = scale;
        }
    }
}