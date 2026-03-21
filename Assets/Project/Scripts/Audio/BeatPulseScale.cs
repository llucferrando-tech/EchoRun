using MoreMountains.Feedbacks;
using UnityEngine;

namespace EchoRun.Level
{
    public sealed class BeatPulseScale : MonoBehaviour
    { 
        [SerializeField] private MMF_Player feedbackPlayer;

        private void Reset()
        {
            feedbackPlayer = GetComponent<MMF_Player>();
        }

        private void OnEnable()
        {
            BeatPulseManager.BeatTriggered += HandleBeatTriggered;
        }

        private void OnDisable()
        {
            BeatPulseManager.BeatTriggered -= HandleBeatTriggered;
        }

        private void HandleBeatTriggered()
        {
            if (feedbackPlayer == null)
                return;

            feedbackPlayer.PlayFeedbacks();
        }
    }
}