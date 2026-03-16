using System;
using UnityEngine;

namespace EchoRun.Level
{
    [Serializable]
    public struct ObstaclePoolEntry
    {
        public ObstacleType obstacleType;
        public ObstaclePool pool;
    }
}