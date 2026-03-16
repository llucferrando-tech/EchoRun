using EchoRun.Core;
using UnityEngine;

namespace EchoRun.Player
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class RunnerMotor : MonoBehaviour
    {
        private const int LeftLane = -1;
        private const int CenterLane = 0;
        private const int RightLane = 1;

        [Header("References")]
        [SerializeField] private PlayerMovementConfig config;
        [SerializeField] private Transform groundCheckOrigin;

        private Rigidbody _rigidbody;
        private int _currentLane = CenterLane;
        private bool _jumpRequested;
        private bool _canMove;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();

            if (config == null)
            {
                Debug.LogError($"{nameof(RunnerMotor)} is missing a {nameof(PlayerMovementConfig)} reference.", this);
            }
        }

        private void OnEnable()
        {
            GameSignals.RunStarted += HandleRunStarted;
            GameSignals.RunEnded += HandleRunEnded;
        }

        private void OnDisable()
        {
            GameSignals.RunStarted -= HandleRunStarted;
            GameSignals.RunEnded -= HandleRunEnded;
        }

        private void FixedUpdate()
        {
            if (!_canMove || config == null)
                return;

            MoveToLane();
            ApplyExtraGravity();
            ProcessJump();
        }

        public void RequestMoveLeft()
        {
            if (!_canMove)
                return;

            int previousLane = _currentLane;
            _currentLane = Mathf.Clamp(_currentLane - 1, LeftLane, RightLane);

            if (_currentLane != previousLane)
            {
                GameSignals.RaiseLaneChanged(_currentLane);
            }
        }

        public void RequestMoveRight()
        {
            if (!_canMove)
                return;

            int previousLane = _currentLane;
            _currentLane = Mathf.Clamp(_currentLane + 1, LeftLane, RightLane);

            if (_currentLane != previousLane)
            {
                GameSignals.RaiseLaneChanged(_currentLane);
            }
        }

        public void RequestJump()
        {
            if (!_canMove)
                return;

            _jumpRequested = true;
            Debug.Log("Jump Requested");
        }

        private void HandleRunStarted()
        {
            _canMove = true;
        }

        private void HandleRunEnded()
        {
            _canMove = false;
            _jumpRequested = false;
        }

        private void MoveToLane()
        {
            Vector3 position = _rigidbody.position;
            float targetX = _currentLane * config.LaneWidth;
            float nextX = Mathf.MoveTowards(position.x, targetX, config.LaneChangeSpeed * Time.fixedDeltaTime);

            _rigidbody.MovePosition(new Vector3(nextX, position.y, position.z));
        }

        private void ProcessJump()
        {
            if (!_jumpRequested)
                return;
            Debug.Log("From request");
            _jumpRequested = false;

            if (!IsGrounded())
                return;
            Debug.Log("Passes ground check");
            Vector3 velocity = GetVelocity();
            velocity.y = 0f;
            SetVelocity(velocity);

            _rigidbody.AddForce(Vector3.up * config.JumpForce, ForceMode.Impulse);
            Debug.Log("Added Velocity");

            GameSignals.RaiseJumpPerformed();
        }

        private void ApplyExtraGravity()
        {
            Vector3 velocity = GetVelocity();

            if (velocity.y >= 0f)
                return;

            _rigidbody.AddForce(
                Physics.gravity * (config.GravityMultiplier - 1f),
                ForceMode.Acceleration);
        }

        private bool IsGrounded()
        {
            Transform origin = groundCheckOrigin != null ? groundCheckOrigin : transform;

            return Physics.Raycast(
                origin.position,
                Vector3.down,
                config.GroundCheckDistance,
                config.GroundLayers,
                QueryTriggerInteraction.Ignore);
        }

        private Vector3 GetVelocity()
        {
#if UNITY_6000_0_OR_NEWER
            return _rigidbody.linearVelocity;
#else
            return _rigidbody.velocity;
#endif
        }

        private void SetVelocity(Vector3 velocity)
        {
#if UNITY_6000_0_OR_NEWER
            _rigidbody.linearVelocity = velocity;
#else
            _rigidbody.velocity = velocity;
#endif
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (config == null)
                return;

            Transform origin = groundCheckOrigin != null ? groundCheckOrigin : transform;

            Gizmos.color = Color.green;
            Gizmos.DrawLine(
                origin.position,
                origin.position + Vector3.down * config.GroundCheckDistance);
        }
#endif
    }
}