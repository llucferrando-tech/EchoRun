using UnityEngine;

namespace EchoRun.Level
{
    public sealed class PooledObstacle : MonoBehaviour
    {
        private ObstaclePool _owningPool;

        public void SetOwningPool(ObstaclePool pool)
        {
            _owningPool = pool;
        }

        public void ReturnToPool()
        {
            if (_owningPool == null)
            {
                gameObject.SetActive(false);
                return;
            }

            _owningPool.Return(this);
        }
    }
}