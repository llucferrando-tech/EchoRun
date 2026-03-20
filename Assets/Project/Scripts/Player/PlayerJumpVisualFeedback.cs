using DG.Tweening;
using UnityEngine;
using EchoRun.Core;

namespace EchoRun.Player
{
    public class PlayerJumpVisualFeedback : MonoBehaviour
    {
        [SerializeField] private Transform visualRoot;
        [SerializeField] private float jumpRotateAmount = -180f;
        [SerializeField] private float jumpRotateDuration = 0.35f;
        [SerializeField] private Ease jumpRotateEase = Ease.OutCubic;

        private Tween _rotateTween;
        private float _currentXRotation;

        private void OnEnable()
        {
            GameSignals.JumpPerformed += HandleJumpPerformed;
        }

        private void OnDisable()
        {
            GameSignals.JumpPerformed -= HandleJumpPerformed;
            _rotateTween?.Kill();
        }

        private void HandleJumpPerformed()
        {
            if (visualRoot == null)
                return;

            _rotateTween?.Kill();

            _currentXRotation += jumpRotateAmount;

            _rotateTween = visualRoot
                .DOLocalRotate(
                    new Vector3(_currentXRotation, 0f, 0f),
                    jumpRotateDuration,
                    RotateMode.Fast)
                .SetEase(jumpRotateEase);
        }
    }
}