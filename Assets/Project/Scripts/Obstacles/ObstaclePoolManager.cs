using System.Collections.Generic;
using UnityEngine;

namespace EchoRun.Level
{
    public sealed class ObstaclePoolManager : MonoBehaviour
    {
        [SerializeField] private List<ObstaclePoolEntry> entries = new();

        public bool TryGetPool(ObstacleType obstacleType, out ObstaclePool pool)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].obstacleType == obstacleType)
                {
                    pool = entries[i].pool;
                    return true;
                }
            }

            pool = null;
            return false;
        }
    }
}