using DG.Tweening;
using UnityEngine;
using EchoRun.Core;
using MoreMountains.Feedbacks;

namespace EchoRun.Player
{
    public class PlayerSquashFeedback : MonoBehaviour
    {
        [SerializeField] private Transform visualRoot;
        [SerializeField] private float jumpSquashDuration = 0.10f;
        [SerializeField] private Vector3 jumpSquashScale = new Vector3(1.08f, 0.88f, 1.08f);
        [SerializeField] private MMF_Player squashJumpFeedback;
        [SerializeField] private MMF_Player squashLaneFeedback;

        private Vector3 _baseScale;
        private Tween _scaleTween;

        private void Awake()
        {
            if (visualRoot != null)
                _baseScale = visualRoot.localScale;
        }

        private void OnEnable()
        {
            GameSignals.JumpPerformed += HandleJumpPerformed;
            GameSignals.LaneChanged += HandleLaneChange;
        }

        private void OnDisable()
        {
            GameSignals.JumpPerformed -= HandleJumpPerformed;
            GameSignals.LaneChanged -= HandleLaneChange;
            _scaleTween?.Kill();
        }

        private void HandleJumpPerformed()
        {
            squashJumpFeedback?.PlayFeedbacks();
        }

        private void HandleLaneChange(int laneIndex)
        {
            Debug.Log($"Change lane: {laneIndex}");
            squashLaneFeedback?.PlayFeedbacks();
        }
    }
}