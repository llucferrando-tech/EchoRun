using EchoRun.Obstacles;
using UnityEngine;

namespace EchoRun.Level
{
    public sealed class ObstacleSpawner : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ObstaclePoolManager obstaclePoolManager;

        [Header("Spawn Settings")]
        [SerializeField] private float laneWidth = 2.5f;
        [SerializeField] private float spawnZ = 30f;

        public float SpawnDistance => spawnZ;

        public void Spawn(LevelEventData eventData, float scrollSpeed)
        {
            if (obstaclePoolManager == null)
            {
                Debug.LogError($"{nameof(ObstacleSpawner)} is missing an {nameof(ObstaclePoolManager)}.", this);
                return;
            }

            if (!obstaclePoolManager.TryGetPool(eventData.obstacleType, out ObstaclePool pool))
            {
                Debug.LogWarning($"No pool mapped for obstacle type {eventData.obstacleType}.", this);
                return;
            }

            PooledObstacle pooledObstacle = pool.Get();
            Transform obstacleTransform = pooledObstacle.transform;

            float x = eventData.lane * laneWidth;
            obstacleTransform.position = new Vector3(x, 0f, spawnZ);
            obstacleTransform.rotation = Quaternion.identity;

            if (pooledObstacle.TryGetComponent(out IObstacleInitializable initializable))
            {
                initializable.Initialize(eventData);
            }

            if (pooledObstacle.TryGetComponent(out MovingObstacle movingObstacle))
            {
                movingObstacle.Activate(scrollSpeed);
            }
        }
    }
}