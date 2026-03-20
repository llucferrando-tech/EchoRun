using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EchoRun.Input
{
    public sealed class RunnerInputReader : MonoBehaviour, IInputReader
    {
        [Header("Thresholds")]
        [SerializeField] private float minSwipeDistance = 80f;
        [SerializeField] private float maxSwipeDuration = 0.45f;
        [SerializeField] private float maxTapDuration = 0.25f;
        [SerializeField] private float maxTapMovement = 25f;

        [Header("Actions")]
        [SerializeField] private InputActionReference pointerPressAction;
        [SerializeField] private InputActionReference pointerPositionAction;

        public event Action TapPressed;
        public event Action SwipedLeft;
        public event Action SwipedRight;
        public event Action SwipedUp;
        public event Action SwipedDown;

        private Vector2 _pressStartPosition;
        private float _pressStartTime;
        private bool _tracking;

        private InputAction PointerPress => pointerPressAction.action;
        private InputAction PointerPosition => pointerPositionAction.action;

        private void OnEnable()
        {
            PointerPress.Enable();
            PointerPosition.Enable();

            PointerPress.started += OnPressStarted;
            PointerPress.canceled += OnPressCanceled;
        }

        private void OnDisable()
        {
            PointerPress.started -= OnPressStarted;
            PointerPress.canceled -= OnPressCanceled;

            PointerPress.Disable();
            PointerPosition.Disable();
        }

        private void OnPressStarted(InputAction.CallbackContext context)
        {
            _tracking = true;
            _pressStartPosition = PointerPosition.ReadValue<Vector2>();
            _pressStartTime = Time.time;
        }

        private void OnPressCanceled(InputAction.CallbackContext context)
        {
            if (!_tracking)
                return;

            _tracking = false;

            Vector2 releasePosition = PointerPosition.ReadValue<Vector2>();
            Vector2 delta = releasePosition - _pressStartPosition;
            float duration = Time.time - _pressStartTime;

            if (duration <= maxTapDuration && delta.magnitude <= maxTapMovement)
            {
                TapPressed?.Invoke();
                return;
            }

            if (duration > maxSwipeDuration || delta.magnitude < minSwipeDistance)
                return;

            float absX = Mathf.Abs(delta.x);
            float absY = Mathf.Abs(delta.y);

            if (absY > absX)
            {
                if (delta.y > 0f)
                    SwipedUp?.Invoke();
                else
                    SwipedDown?.Invoke();
            }
            else
            {
                if (delta.x > 0f)
                    SwipedRight?.Invoke();
                else
                    SwipedLeft?.Invoke();
            }
        }
    }
}