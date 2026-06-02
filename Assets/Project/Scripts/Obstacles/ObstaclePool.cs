using System.Collections.Generic;
using UnityEngine;

namespace EchoRun.Level
{
    public sealed class ObstaclePool : MonoBehaviour
    {
        [SerializeField] private GameObject prefab;
        [SerializeField] private int initialSize = 10;

        private readonly Queue<PooledObstacle> _available = new();

        public GameObject Prefab => prefab;

        private void Awake()
        {
            Prewarm();
        }

        private void Prewarm()
        {
            if (prefab == null)
            {
                Debug.LogError($"{nameof(ObstaclePool)} is missing a prefab.", this);
                return;
            }

            for (int i = 0; i < initialSize; i++)
            {
                CreateAndStore();
            }
        }

        public PooledObstacle Get()
        {
            if (_available.Count == 0)
            {
                CreateAndStore();
            }

            PooledObstacle instance = _available.Dequeue();
            instance.gameObject.SetActive(true);
            return instance;
        }

        public void Return(PooledObstacle instance)
        {
            if (instance == null)
                return;

            instance.gameObject.SetActive(false);
            instance.transform.SetParent(transform);
            _available.Enqueue(instance);

            Debug.Log("Testing");
        }

        private void CreateAndStore()
        {
            GameObject instanceObject = Instantiate(prefab, transform);
            instanceObject.SetActive(false);

            PooledObstacle pooledObstacle = instanceObject.GetComponent<PooledObstacle>();

            if (pooledObstacle == null)
            {
                pooledObstacle = instanceObject.AddComponent<PooledObstacle>();
            }

            pooledObstacle.SetOwningPool(this);
            _available.Enqueue(pooledObstacle);
        }
    }
}