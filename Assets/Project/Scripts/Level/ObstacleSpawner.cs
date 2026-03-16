using UnityEngine;

namespace EchoRun.Level
{
    public sealed class ObstacleSpawner : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ObstacleCatalog obstacleCatalog;

        [Header("Spawn Settings")]
        [SerializeField] private float laneWidth = 2.5f;
        [SerializeField] private float spawnZ = 30f;
        [SerializeField] private Transform obstacleParent;

        public void Spawn(LevelEventData eventData)
        {
            if (obstacleCatalog == null)
            {
                Debug.LogError($"{nameof(ObstacleSpawner)} is missing an {nameof(ObstacleCatalog)}.", this);
                return;
            }

            if (!obstacleCatalog.TryGetPrefab(eventData.obstacleType, out GameObject prefab))
            {
                Debug.LogWarning($"No prefab mapped for obstacle type {eventData.obstacleType}.", this);
                return;
            }

            float x = eventData.lane * laneWidth;
            Vector3 spawnPosition = new Vector3(x, 0f, spawnZ);

            Instantiate(prefab, spawnPosition, Quaternion.identity, obstacleParent);
        }
    }
}