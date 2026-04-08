using System;
using UnityEngine;

namespace EchoRun.Level
{
    public sealed class LevelSession : MonoBehaviour
    {
        [SerializeField] private LevelDefinition currentLevel;

        public event Action<LevelDefinition> LevelChanged;

        public LevelDefinition CurrentLevel => currentLevel;

        private void Start()
        {
            if (currentLevel != null)
            {
                LevelChanged?.Invoke(currentLevel);
            }
        }

        public void SetLevel(LevelDefinition newLevel)
        {
            if (newLevel == currentLevel)
                return;

            currentLevel = newLevel;
            LevelChanged?.Invoke(currentLevel);
        }
    }
}