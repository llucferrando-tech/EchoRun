using System.Collections.Generic;
using UnityEngine;
using EchoRun.Level;
using EchoRun.Core;
namespace EchoRun.UI
{
    public sealed class SongSelectOverlayController : MonoBehaviour
    {
        [SerializeField] private Transform contentRoot;
        [SerializeField] private SongSelectItemUI itemPrefab;
        [SerializeField] private List<LevelDefinition> availableLevels = new();

        private readonly List<SongSelectItemUI> _spawnedItems = new();

        private void Start()
        {
            Build();
        }

        private void Build()
        {
            Clear();

            for (int i = 0; i < availableLevels.Count; i++)
            {
                LevelDefinition level = availableLevels[i];
                SongSelectItemUI item = Instantiate(itemPrefab, contentRoot);
                item.Bind(level, HandleSelected);
                _spawnedItems.Add(item);
            }
        }

        private void HandleSelected(LevelDefinition level)
        {
            GameSignals.RaiseLevelSelected(level);
        }

        private void Clear()
        {
            for (int i = 0; i < _spawnedItems.Count; i++)
            {
                if (_spawnedItems[i] != null)
                    Destroy(_spawnedItems[i].gameObject);
            }

            _spawnedItems.Clear();
        }
    }
}