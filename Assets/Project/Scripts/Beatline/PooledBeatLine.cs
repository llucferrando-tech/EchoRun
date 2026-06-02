using UnityEngine;

namespace EchoRun.Level
{
    public sealed class PooledBeatLine : MonoBehaviour
    {
        private BeatLinePool _pool;

        public void SetPool(BeatLinePool pool)
        {
            _pool = pool;
        }

        public void ReturnToPool()
        {
            if (_pool != null)
                _pool.Return(this);
            else
                gameObject.SetActive(false);
        }
    }
}