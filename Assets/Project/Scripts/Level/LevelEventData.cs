using System;
using UnityEngine;

namespace EchoRun.Level
{
    [Serializable]
    public struct LevelEventData
    {
        [Min(0f)]
        public float time;

        [Range(-1, 1)]
        public int lane;

        public ObstacleType obstacleType;
    }
}