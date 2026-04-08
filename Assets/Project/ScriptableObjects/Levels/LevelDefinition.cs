using System.Collections.Generic;
using UnityEngine;

namespace EchoRun.Level
{
    [CreateAssetMenu(
        fileName = "LevelDefinition",
        menuName = "EchoRun/Level/Level Definition")]
    public sealed class LevelDefinition : ScriptableObject
    {
        [SerializeField] private string levelId;
        [SerializeField] private string songName;
        [SerializeField] private float bpm;
        [SerializeField] private float scrollSpeed = 8f;
        [SerializeField] private List<float> beatTimes = new();
        [SerializeField] private List<LevelEventData> events = new();

        public string LevelId => levelId;
        public string SongName => songName;
        public float Bpm => bpm;
        public float ScrollSpeed => scrollSpeed;
        public IReadOnlyList<float> BeatTimes => beatTimes;
        public IReadOnlyList<LevelEventData> Events => events;

#if UNITY_EDITOR
        public void EditorSetImportedData(
            float newBpm,
            float newScrollSpeed,
            List<float> newBeatTimes,
            List<LevelEventData> newEvents)
        {
            bpm = newBpm;
            scrollSpeed = newScrollSpeed;
            beatTimes = newBeatTimes;
            events = newEvents;
        }
#endif
    }
}