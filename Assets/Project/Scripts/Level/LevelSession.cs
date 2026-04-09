using System;
using UnityEngine;

namespace EchoRun.Level
{
    public sealed class LevelSession : MonoBehaviour
    {
        [SerializeField] private LevelDefinition currentLevel;

        public event Action<LevelDefinition> LevelChanged;

        public LevelDefinition CurrentLevel => currentLevel;

        public void SetLevel(LevelDefinition newLevel)
        {
            if (newLevel == currentLevel)
                return;

            currentLevel = newLevel;
            LevelChanged?.Invoke(currentLevel);
        }
    }
}