namespace EchoRun.Level
{
    public interface IPoolableObstacle
    {
        void OnSpawned(LevelEventData eventData, float moveSpeed);
        void ReturnToPool();
    }
}