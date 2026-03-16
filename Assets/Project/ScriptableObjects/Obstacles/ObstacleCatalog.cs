using System.Collections.Generic;
using UnityEngine;

namespace EchoRun.Level
{
    [CreateAssetMenu(
        fileName = "ObstacleCatalog",
        menuName = "EchoRun/Level/Obstacle Catalog")]
    public sealed class ObstacleCatalog : ScriptableObject
    {
        [SerializeField] private List<ObstacleCatalogEntry> entries = new();

        public bool TryGetPrefab(ObstacleType obstacleType, out GameObject prefab)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].obstacleType == obstacleType)
                {
                    prefab = entries[i].prefab;
                    return true;
                }
            }

            prefab = null;
            return false;
        }
    }
}