using System;

namespace EchoRun.Core
{
    public static class GameSignals
    {
        public static event Action TapToStartRequested;
        public static event Action RetryRequested;

        public static event Action RunStarted;
        public static event Action RunEnded;
        public static event Action PlayerDied;

        public static event Action JumpPerformed;
        public static event Action<int> LaneChanged;
        public static event Action<int> ScoreChanged;

        public static void RaiseTapToStartRequested() => TapToStartRequested?.Invoke();
        public static void RaiseRetryRequested() => RetryRequested?.Invoke();

        public static void RaiseRunStarted() => RunStarted?.Invoke();
        public static void RaiseRunEnded() => RunEnded?.Invoke();
        public static void RaisePlayerDied() => PlayerDied?.Invoke();

        public static void RaiseJumpPerformed() => JumpPerformed?.Invoke();
        public static void RaiseLaneChanged(int laneIndex) => LaneChanged?.Invoke(laneIndex);
        public static void RaiseScoreChanged(int score) => ScoreChanged?.Invoke(score);
    }
}