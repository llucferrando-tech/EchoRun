using System;

namespace EchoRun.Level
{
    [Serializable]
    public sealed class LevelJsonData
    {
        public string songName;
        public float bpm;
        public float scrollSpeed;
        public LevelEventJsonData[] events;
    }

    [Serializable]
    public sealed class LevelEventJsonData
    {
        public float time;
        public int lane;
        public string obstacleType;
        public float duration;
    }
}