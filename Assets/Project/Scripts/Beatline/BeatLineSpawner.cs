using UnityEngine;

namespace EchoRun.Level
{
    public sealed class BeatLineSpawner : MonoBehaviour
    {
        [Header("Pool")]
        [SerializeField] private BeatLinePool beatLinePool;

        [Header("Spawn")]
        [SerializeField] private float spawnZ = 30f;
        [SerializeField] private float y = 0.02f;
        [SerializeField] private float width = 7.5f;
        [SerializeField] private float depth = 0.08f;

        public float SpawnDistance => spawnZ;

        public void Spawn(float scrollSpeed)
        {
            if (beatLinePool == null)
            {
                Debug.LogWarning($"{nameof(BeatLineSpawner)} has no pool assigned.", this);
                return;
            }

            PooledBeatLine pooledBeatLine = beatLinePool.Get();

            if (pooledBeatLine == null)
                return;

            Transform beatLineTransform = pooledBeatLine.transform;

            beatLineTransform.position = new Vector3(0f, y, spawnZ);
            beatLineTransform.rotation = Quaternion.identity;

            Vector3 scale = beatLineTransform.localScale;
            scale.x = width;
            scale.z = depth;
            beatLineTransform.localScale = scale;

            if (pooledBeatLine.TryGetComponent(out BeatLineMover mover))
                mover.Activate(scrollSpeed);
        }
    }
}