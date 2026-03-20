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

        [Header("Slide")]
        [SerializeField] private float slideDuration = 0.6f;
        [SerializeField] private CapsuleCollider standingCollider;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private float slidingVisualY = -0.5f;

        private Rigidbody _rigidbody;
        private int _currentLane = CenterLane;
        private bool _jumpRequested;
        private bool _canMove;
        private bool _isSliding;
        private bool _isFastFalling;
        private float _slideTimer;
        private Vector3 _initialVisualLocalPosition;

        public bool IsSliding => _isSliding;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();

            if (config == null)
            {
                Debug.LogError($"{nameof(RunnerMotor)} is missing a {nameof(PlayerMovementConfig)} reference.", this);
            }

            if (visualRoot != null)
            {
                _initialVisualLocalPosition = visualRoot.localPosition;
            }

            SetSlidingState(false);
        }

        private void OnEnable()
        {
            GameSignals.CountdownStarted += HandleCountdownStarted;
            GameSignals.RunEnded += HandleRunEnded;
        }

        private void OnDisable()
        {
            GameSignals.CountdownStarted -= HandleCountdownStarted;
            GameSignals.RunEnded -= HandleRunEnded;
        }

        private void Update()
        {
            if (!_canMove)
                return;

            UpdateSlideTimer();
        }

        private void FixedUpdate()
        {
            if (!_canMove || config == null)
                return;

            MoveToLane();
            ProcessJump();
            ApplyVerticalForces();
            RefreshAirStates();
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

            if (_isSliding)
            {
                CancelSlide();
            }

            _jumpRequested = true;
        }

        public void RequestSlide()
        {
            if (!_canMove)
                return;

            if (!IsGrounded())
            {
                StartFastFall();
                return;
            }

            _slideTimer = slideDuration;

            if (_isSliding)
                return;

            SetSlidingState(true);
            GameSignals.RaiseSlideStarted();
        }

        private void HandleCountdownStarted()
        {
            _canMove = true;
        }

        private void HandleRunEnded()
        {
            _canMove = false;
            _jumpRequested = false;
            _slideTimer = 0f;
            _isFastFalling = false;

            if (_isSliding)
            {
                CancelSlide();
            }
        }

        private void UpdateSlideTimer()
        {
            if (!_isSliding)
                return;

            _slideTimer -= Time.deltaTime;

            if (_slideTimer > 0f)
                return;

            CancelSlide();
        }

        private void CancelSlide()
        {
            if (!_isSliding)
                return;

            _slideTimer = 0f;
            SetSlidingState(false);
            GameSignals.RaiseSlideEnded();
        }

        private void SetSlidingState(bool sliding)
        {
            _isSliding = sliding;

            if (sliding)
            {
                standingCollider.center = new Vector3(0f, -0.5f, 0f);
                standingCollider.height = 1f;
            }
            else
            {
                standingCollider.center = Vector3.zero;
                standingCollider.height = 2f;
            }

            if (visualRoot != null)
            {
                Vector3 pos = _initialVisualLocalPosition;
                if (sliding)
                    pos.y = slidingVisualY;

                visualRoot.localPosition = pos;
            }
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

            _jumpRequested = false;

            if (!IsGrounded())
                return;

            _isFastFalling = false;

            Vector3 velocity = GetVelocity();
            velocity.y = 0f;
            SetVelocity(velocity);

            _rigidbody.AddForce(Vector3.up * config.JumpForce, ForceMode.Impulse);
            GameSignals.RaiseJumpPerformed();
        }

        private void ApplyVerticalForces()
        {
            Vector3 velocity = GetVelocity();

            if (IsGrounded() && velocity.y <= 0.05f)
            {
                _isFastFalling = false;
                return;
            }

            float multiplier;

            if (_isFastFalling)
            {
                multiplier = config.FastFallGravityMultiplier;
            }
            else if (velocity.y > 0f)
            {
                multiplier = config.RiseGravityMultiplier;
            }
            else
            {
                multiplier = config.FallGravityMultiplier;
            }

            if (multiplier <= 1f)
                return;

            _rigidbody.AddForce(
                Physics.gravity * (multiplier - 1f),
                ForceMode.Acceleration);
        }

        private void StartFastFall()
        {
            Vector3 velocity = GetVelocity();

            if (IsGrounded())
                return;

            if (velocity.y > 0f)
            {
                velocity.y *= config.JumpCutMultiplier;
                SetVelocity(velocity);
            }

            _isFastFalling = true;
        }

        private void RefreshAirStates()
        {
            if (IsGrounded() && GetVelocity().y <= 0.05f)
            {
                _isFastFalling = false;
            }
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