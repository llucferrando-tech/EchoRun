using EchoRun.Core;
using UnityEngine;
using EchoRun.Audio;

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
        [SerializeField] public SongTimeProvider songTimeProvider;

        [Header("Jump")]
        [SerializeField] private float jumpBufferTime = 0.12f;
        [SerializeField] private float postJumpGroundLockTime = 0.12f;

        [Header("Slide")]
        [SerializeField] private float slideDuration = 0.6f;
        [SerializeField] private BoxCollider standingCollider;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Vector3 standingColliderCenter = Vector3.zero;
        [SerializeField] private Vector3 standingColliderSize = new Vector3(1f, 2f, 1f);

        [SerializeField] private Vector3 slidingColliderCenter = new Vector3(0f, -0.5f, 0f);
        [SerializeField] private Vector3 slidingColliderSize = new Vector3(1f, 1f, 1f);
        
        [SerializeField] private float slidingVisualY = -0.5f;

        private Rigidbody _rigidbody;
        private int _currentLane = CenterLane;
        private bool _jumpRequested;
        private bool _canMove;
        private bool _isSliding;
        private bool _isFastFalling;
        private float _slideTimer;
        private float _lastJumpPressedTime = -999f;
        private Vector3 _initialVisualLocalPosition;

        public bool IsSliding => _isSliding;

        private float _lastLaneChangeTime = -999f;
        private float _lastJumpPerformedTime = -999f;
        private float _lastSlideStartedTime = -999f;

        public int CurrentLane => _currentLane;
        public float LastLaneChangeTime => _lastLaneChangeTime;
        public float LastJumpPerformedTime => _lastJumpPerformedTime;
        public float LastSlideStartedTime => _lastSlideStartedTime;

        private float _lastFastFallTime = -999f;
        public float LastFastFallTime => _lastFastFallTime;

        private bool _bufferedJumpUsedThisAirTime;
        private bool _hasLeftGroundSinceJump;
        private float _ignoreGroundedUntilTime = -999f;

        private int _originalLayer;
        private int _slidingLayer;
        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();

           if (standingCollider != null)
            {
            standingColliderCenter = standingCollider.center;
            standingColliderSize = standingCollider.size;
            }

            if (config == null)
                Debug.LogError($"{nameof(RunnerMotor)} is missing a {nameof(PlayerMovementConfig)} reference.", this);

            if (visualRoot != null)
                _initialVisualLocalPosition = visualRoot.localPosition;

            SetSlidingState(false);
        }

        private void OnEnable()
        {
            GameSignals.CountdownStarted += HandleCountdownStarted;
            GameSignals.RunEnded += HandleRunEnded;
            GameSignals.ContinueRunGranted += HandleContinueRunGranted;
        }

        private void OnDisable()
        {
            GameSignals.CountdownStarted -= HandleCountdownStarted;
            GameSignals.RunEnded -= HandleRunEnded;
            GameSignals.ContinueRunGranted -= HandleContinueRunGranted;
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
                _lastLaneChangeTime = GetCurrentActionTime();
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
                _lastLaneChangeTime = GetCurrentActionTime();
                GameSignals.RaiseLaneChanged(_currentLane);
            }
        }

        public void RequestJump()
        {
            if (!_canMove)
                return;

            if (_isSliding)
                CancelSlide();

            bool groundedForJump = IsGroundedForJump();

            if (groundedForJump)
            {
                _jumpRequested = true;
                _lastJumpPressedTime = -999f;
                return;
            }

            // Airborne: allow only one buffered input per airtime.
            // This lets the player swipe shortly before landing,
            // but prevents spam from stacking repeated jumps.
            if (_bufferedJumpUsedThisAirTime)
                return;

            _lastJumpPressedTime = GetCurrentActionTime();
            _bufferedJumpUsedThisAirTime = true;
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

            _lastSlideStartedTime = GetCurrentActionTime();
            SetSlidingState(true);
            GameSignals.RaiseSlideStarted();
        }

        private void HandleCountdownStarted()
        {
            ResetMovementStateForStart();
            _canMove = true;
        }

        private void HandleRunEnded()
        {
            _canMove = false;
            ClearActionState();
        }

        private void HandleContinueRunGranted()
        {
            ResetMovementStateForStart();
            _canMove = true;

            Debug.Log("RunnerMotor continued. Movement re-enabled.");
        }

        private void ResetMovementStateForStart()
        {
            _jumpRequested = false;
            _slideTimer = 0f;
            _isFastFalling = false;
            _lastJumpPressedTime = -999f;
            _bufferedJumpUsedThisAirTime = false;
            _hasLeftGroundSinceJump = false;
            _ignoreGroundedUntilTime = -999f;

            if (_isSliding)
                CancelSlide();

            Vector3 velocity = GetVelocity();
            velocity.x = 0f;
            velocity.z = 0f;
            SetVelocity(velocity);
        }

        private void ClearActionState()
        {
            _jumpRequested = false;
            _slideTimer = 0f;
            _isFastFalling = false;
            _lastJumpPressedTime = -999f;
            _bufferedJumpUsedThisAirTime = false;
            _hasLeftGroundSinceJump = false;
            _ignoreGroundedUntilTime = -999f;

            if (_isSliding)
                CancelSlide();
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

            if (standingCollider != null)
            {
                standingCollider.center = sliding ? slidingColliderCenter : standingColliderCenter;
                standingCollider.size = sliding ? slidingColliderSize : standingColliderSize;
            }

            if (visualRoot != null)
            {
                Vector3 pos = _initialVisualLocalPosition;

                if (sliding)
                    pos.y = slidingVisualY;

                visualRoot.localPosition = pos;
            }
        }
        private bool HasBufferedJump()
        {
            return GetCurrentActionTime() - _lastJumpPressedTime <= jumpBufferTime;
        }

        private void ConsumeJumpBuffer()
        {
            _jumpRequested = false;
            _lastJumpPressedTime = -999f;
        }

        private void MoveToLane()
        {
            Vector3 position = _rigidbody.position;
            float targetX = _currentLane * config.LaneWidth;
            float nextX = Mathf.MoveTowards(position.x, targetX, config.LaneChangeSpeed * Time.fixedDeltaTime);

            _rigidbody.MovePosition(new Vector3(nextX, position.y, position.z));
        }

        private bool IsGroundedForJump()
        {
            if (Time.time < _ignoreGroundedUntilTime)
                return false;

            return IsGrounded();
        }

        private void ProcessJump()
        {
            bool hasBufferedJump =
                _lastJumpPressedTime > -900f &&
                GetCurrentActionTime() - _lastJumpPressedTime <= jumpBufferTime;

            if (!_jumpRequested && !hasBufferedJump)
                return;

            if (!IsGroundedForJump())
                return;

            _jumpRequested = false;
            _lastJumpPressedTime = -999f;
            _bufferedJumpUsedThisAirTime = false;

            _isFastFalling = false;

            Vector3 velocity = GetVelocity();
            velocity.y = 0f;
            SetVelocity(velocity);

            _rigidbody.AddForce(Vector3.up * config.JumpForce, ForceMode.Impulse);

            _ignoreGroundedUntilTime = Time.time + postJumpGroundLockTime;
            _hasLeftGroundSinceJump = false;

            _lastJumpPerformedTime = GetCurrentActionTime();
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
                multiplier = config.FastFallGravityMultiplier;
            else if (velocity.y > 0f)
                multiplier = config.RiseGravityMultiplier;
            else
                multiplier = config.FallGravityMultiplier;

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

            bool wasFastFalling = _isFastFalling;

            if (velocity.y > 0f)
            {
                velocity.y *= config.JumpCutMultiplier;
                SetVelocity(velocity);
            }

            _isFastFalling = true;

            if (!wasFastFalling)
            {
                _lastFastFallTime = GetCurrentActionTime();
                GameSignals.RaiseFastFallStarted();
            }
        }

        private void RefreshAirStates()
        {
            bool groundedRaw = IsGrounded();

            if (!groundedRaw)
            {
                _hasLeftGroundSinceJump = true;
                return;
            }

            if (GetVelocity().y <= 0.05f)
            {
                _isFastFalling = false;

                if (_hasLeftGroundSinceJump && Time.time >= _ignoreGroundedUntilTime)
                {
                    _bufferedJumpUsedThisAirTime = false;
                    _hasLeftGroundSinceJump = false;
                }
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

        public float GetCurrentActionTime()
        {
            if (songTimeProvider != null)
                return songTimeProvider.GetSongTime();

            return Time.time;
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