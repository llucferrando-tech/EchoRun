using UnityEngine;
using EchoRun.Level;
using EchoRun.Audio;
using EchoRun.Core;

namespace EchoRun.Debugging
{
    public sealed class BeatDebugPlayer : MonoBehaviour
    {
       [SerializeField] private SongTimeProvider songTimeProvider;

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
            float songTime = songTimeProvider != null ? songTimeProvider.GetSongTime() : -1f;
            //Debug.Log($"BEAT NOW -> songTime={songTime:F3}");
        }
    }
}