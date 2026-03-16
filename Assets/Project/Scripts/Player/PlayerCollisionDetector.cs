using EchoRun.Core;
using EchoRun.Obstacles;
using UnityEngine;

namespace EchoRun.Player
{
    public sealed class PlayerCollisionDetector : MonoBehaviour
    {
        private bool _hasCollided;

        private void OnCollisionEnter(Collision collision)
        {
            TryHandleCollision(collision.collider);
        }

        private void OnTriggerEnter(Collider other)
        {
            TryHandleCollision(other);
        }

        private void TryHandleCollision(Collider other)
        {
            if (_hasCollided)
                return;

            if (other.GetComponentInParent<IObstacle>() == null)
                return;

            _hasCollided = true;
            GameSignals.RaisePlayerDied();
        }

        private void OnEnable()
        {
            GameSignals.RunStarted += HandleRunStarted;
        }

        private void OnDisable()
        {
            GameSignals.RunStarted -= HandleRunStarted;
        }

        private void HandleRunStarted()
        {
            _hasCollided = false;
        }
    }
}