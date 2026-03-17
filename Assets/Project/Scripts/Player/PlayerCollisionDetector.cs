using EchoRun.Core;
using EchoRun.Obstacles;
using UnityEngine;

namespace EchoRun.Player
{
    public sealed class PlayerCollisionDetector : MonoBehaviour
    {
        private bool _hasCollided;
        private bool _canCollide;

        private void OnEnable()
        {
            GameSignals.CountdownStarted += HandleCountdownStarted;
            GameSignals.GameplayStarted += HandleGameplayStarted;
            GameSignals.RunEnded += HandleRunEnded;
        }

        private void OnDisable()
        {
            GameSignals.CountdownStarted -= HandleCountdownStarted;
            GameSignals.GameplayStarted -= HandleGameplayStarted;
            GameSignals.RunEnded -= HandleRunEnded;
        }

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
            if (!_canCollide || _hasCollided)
                return;

            if (other.GetComponentInParent<IObstacle>() == null)
                return;

            _hasCollided = true;
            GameSignals.RaisePlayerDied();
        }

        private void HandleCountdownStarted()
        {
            _hasCollided = false;
            _canCollide = false;
        }

        private void HandleGameplayStarted()
        {
            _canCollide = true;
        }

        private void HandleRunEnded()
        {
            _canCollide = false;
        }
    }
}