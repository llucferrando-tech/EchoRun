using EchoRun.Level;
using EchoRun.Audio;
using UnityEngine;

namespace EchoRun.Gameplay
{
    public sealed class BeatTimingScorer : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SongTimeProvider songTimeProvider;

        [Header("Timing Windows")]
        [SerializeField] private float perfectWindow = 0.06f;
        [SerializeField] private float goodWindow = 0.12f;

        private bool _hasBeat;
        private float _lastBeatSongTime = -999f;
        private float _previousBeatSongTime = -999f;
        private float _lastBeatInterval = 0.5f;

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
            if (songTimeProvider == null)
                return;

            float currentSongTime = songTimeProvider.GetSongTime();

            if (_hasBeat)
            {
                _previousBeatSongTime = _lastBeatSongTime;
                _lastBeatSongTime = currentSongTime;

                float interval = _lastBeatSongTime - _previousBeatSongTime;
                if (interval > 0.05f)
                {
                    _lastBeatInterval = interval;
                }
            }
            else
            {
                _hasBeat = true;
                _lastBeatSongTime = currentSongTime;
                _previousBeatSongTime = currentSongTime - _lastBeatInterval;
            }
        }

        public BeatAccuracy EvaluateNow()
        {
            if (songTimeProvider == null)
                return BeatAccuracy.None;

            return Evaluate(songTimeProvider.GetSongTime());
        }

        public BeatAccuracy Evaluate(float actionSongTime)
        {
            if (!_hasBeat)
            {
                Debug.Log("[BeatTiming] No beats registered yet.");
                return BeatAccuracy.None;
            }

            float distanceToLastBeat = Mathf.Abs(actionSongTime - _lastBeatSongTime);

            float predictedNextBeat = _lastBeatSongTime + _lastBeatInterval;
            float distanceToNextBeat = Mathf.Abs(predictedNextBeat - actionSongTime);

            float nearestDistance = Mathf.Min(distanceToLastBeat, distanceToNextBeat);

            Debug.Log(
                $"[BeatTiming] ActionTime={actionSongTime:F3} | " +
                $"LastBeat={_lastBeatSongTime:F3} | " +
                $"NextBeat={predictedNextBeat:F3} | " +
                $"DistLast={distanceToLastBeat:F3} | " +
                $"DistNext={distanceToNextBeat:F3} | " +
                $"Nearest={nearestDistance:F3}"
            );

            if (nearestDistance <= perfectWindow)
            {
                Debug.Log($"[BeatTiming] PERFECT ({nearestDistance:F3})");
                return BeatAccuracy.Perfect;
            }

            if (nearestDistance <= goodWindow)
            {
                Debug.Log($"[BeatTiming] GOOD ({nearestDistance:F3})");
                return BeatAccuracy.Good;
            }

            Debug.Log($"[BeatTiming] MISS ({nearestDistance:F3})");
            return BeatAccuracy.None;
        }
    }
}