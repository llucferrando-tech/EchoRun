#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace EchoRun.Level.Editor
{
    public static class LevelJsonImporter
    {
        [MenuItem("EchoRun/Import Level JSON")]
        private static void ImportLevelJson()
        {
            string jsonPath = EditorUtility.OpenFilePanel("Select Level JSON", Application.dataPath, "json");

            if (string.IsNullOrWhiteSpace(jsonPath))
                return;

            string jsonText = System.IO.File.ReadAllText(jsonPath);
            LevelJsonData jsonData = JsonUtility.FromJson<LevelJsonData>(jsonText);

            if (jsonData == null)
            {
                Debug.LogError("Failed to parse selected JSON file.");
                return;
            }

            List<float> beatTimes = new();
            if (jsonData.beatTimes != null)
            {
                for (int i = 0; i < jsonData.beatTimes.Length; i++)
                {
                    beatTimes.Add(Mathf.Max(0f, jsonData.beatTimes[i]));
                }
            }

            List<LevelEventData> events = new();

            if (jsonData.events != null)
            {
                for (int i = 0; i < jsonData.events.Length; i++)
                {
                    LevelEventJsonData sourceEvent = jsonData.events[i];

                    if (!Enum.TryParse(sourceEvent.obstacleType, true, out ObstacleType obstacleType))
                    {
                        Debug.LogWarning($"Unknown obstacle type '{sourceEvent.obstacleType}' at index {i}. Skipping.");
                        continue;
                    }

                    events.Add(new LevelEventData
                    {
                        time = Mathf.Max(0f, sourceEvent.time),
                        lane = Mathf.Clamp(sourceEvent.lane, -1, 1),
                        obstacleType = obstacleType,
                        duration = Mathf.Max(0f, sourceEvent.duration)
                    });
                }
            }

            events.Sort((a, b) => a.time.CompareTo(b.time));
            beatTimes.Sort((a, b) => a.CompareTo(b));

            LevelDefinition asset = ScriptableObject.CreateInstance<LevelDefinition>();
            asset.EditorSetImportedData(
                jsonData.bpm,
                jsonData.scrollSpeed,
                beatTimes,
                events);

            string defaultFileName = System.IO.Path.GetFileNameWithoutExtension(jsonPath);

            string assetPath = EditorUtility.SaveFilePanelInProject(
                "Save Level Definition",
                string.IsNullOrWhiteSpace(defaultFileName) ? "NewLevelDefinition" : defaultFileName,
                "asset",
                "Choose where to save the LevelDefinition asset.");

            if (string.IsNullOrWhiteSpace(assetPath))
                return;

            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = asset;
            Debug.Log($"Imported level JSON into LevelDefinition: {assetPath}");
        }
    }
}
#endif