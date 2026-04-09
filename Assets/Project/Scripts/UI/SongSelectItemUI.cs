using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EchoRun.Level;
using EchoRun.Gameplay;

namespace EchoRun.UI
{
    public sealed class SongSelectItemUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text songNameText;
        [SerializeField] private TMP_Text highScoreText;
        [SerializeField] private Button button;

        private LevelDefinition _level;
        private Action<LevelDefinition> _onSelected;

        public void Bind(LevelDefinition level, Action<LevelDefinition> onSelected)
        {
            _level = level;
            _onSelected = onSelected;

            if (songNameText != null)
                songNameText.text = level != null ? level.SongName : "Unknown";

            if (highScoreText != null)
                highScoreText.text = level != null ? ScorePersistence.GetHighScore(level).ToString() : "0";

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(HandleClick);
            }
        }

        private void HandleClick()
        {
            if (_level == null)
                return;

            _onSelected?.Invoke(_level);
        }
    }
}