using System;
using UnityEngine;

namespace EchoRun.Level
{
    [Serializable]
    public struct ObstacleCatalogEntry
    {
        public ObstacleType obstacleType;
        public GameObject prefab;
    }
}