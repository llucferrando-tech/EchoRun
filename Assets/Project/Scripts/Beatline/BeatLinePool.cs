using System.Collections.Generic;
using UnityEngine;

namespace EchoRun.Level
{
    public sealed class BeatLinePool : MonoBehaviour
    {
        [SerializeField] private PooledBeatLine prefab;
        [SerializeField] private int initialSize = 64;
        [SerializeField] private bool allowExpansion = true;

        private readonly Queue<PooledBeatLine> _available = new();

        private void Awake()
        {
            Warmup();
        }

        private void Warmup()
        {
            if (prefab == null)
            {
                Debug.LogError($"{nameof(BeatLinePool)} is missing a prefab.", this);
                return;
            }

            for (int i = 0; i < initialSize; i++)
            {
                CreateInstance();
            }
        }

        public PooledBeatLine Get()
        {
            if (_available.Count > 0)
            {
                PooledBeatLine pooled = _available.Dequeue();
                pooled.gameObject.SetActive(true);
                return pooled;
            }

            if (allowExpansion)
                return CreateInstance(true);

            Debug.LogWarning($"{nameof(BeatLinePool)} ran out of beat lines.", this);
            return null;
        }

        public void Return(PooledBeatLine pooled)
        {
            if (pooled == null)
                return;

            pooled.gameObject.SetActive(false);
            pooled.transform.SetParent(transform);
            _available.Enqueue(pooled);
        }

        private PooledBeatLine CreateInstance(bool active = false)
        {
            PooledBeatLine instance = Instantiate(prefab, transform);
            instance.SetPool(this);
            instance.gameObject.SetActive(active);

            if (!active)
                _available.Enqueue(instance);

            return instance;
        }
    }
}