using EchoRun.Core;
using MoreMountains.Feedbacks;
using UnityEngine;
using UnityEngine.Events;

namespace EchoRun.Player
{
    public sealed class SlideStartedListener : MonoBehaviour
    {
        //[SerializeField] private UnityEvent onSlideStarted;
        [SerializeField] MMF_Player slideFeedbacks;

        private void OnEnable()
        {
            GameSignals.SlideStarted += HandleSlideStarted;
        }

        private void OnDisable()
        {
            GameSignals.SlideStarted -= HandleSlideStarted;
        }

        private void HandleSlideStarted()
        {
            slideFeedbacks?.PlayFeedbacks();
        }
    }
}