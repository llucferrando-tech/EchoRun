using UnityEngine;
using EchoRun.Level;

namespace EchoRun.Gameplay
{
    public static class ScorePersistence
    {
        public static int GetHighScore(LevelDefinition level)
        {
            if (level == null || string.IsNullOrWhiteSpace(level.LevelId))
                return 0;

            return PlayerPrefs.GetInt(GetHighScoreKey(level), 0);
        }

        public static void SetHighScore(LevelDefinition level, int score)
        {
            if (level == null || string.IsNullOrWhiteSpace(level.LevelId))
                return;

            int currentHighScore = GetHighScore(level);

            if (score <= currentHighScore)
                return;

            PlayerPrefs.SetInt(GetHighScoreKey(level), score);
            PlayerPrefs.Save();
        }

        public static void ResetHighScore(LevelDefinition level)
        {
            if (level == null || string.IsNullOrWhiteSpace(level.LevelId))
                return;

            PlayerPrefs.DeleteKey(GetHighScoreKey(level));
            PlayerPrefs.Save();
        }

        private static string GetHighScoreKey(LevelDefinition level)
        {
            return $"HIGHSCORE_{level.LevelId}";
        }
    }
}