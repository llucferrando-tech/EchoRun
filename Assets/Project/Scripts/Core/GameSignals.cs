using System;
using Unity.VisualScripting;

namespace EchoRun.Core
{
    public static class GameSignals
    {
        public static event Action TapToStartRequested;
        public static event Action RetryRequested;

        public static event Action CountdownStarted;
        public static event Action<int> CountdownTicked;
        public static event Action CountdownGo;
        public static event Action GameplayStarted;
        public static event Action RunEnded;
        public static event Action PlayerDied;

        public static event Action JumpPerformed;
        public static event Action<int> LaneChanged;
        public static event Action SlideStarted;
        public static event Action SlideEnded;
        public static event Action FastFallStarted;
        public static event Action<EchoRun.Level.LevelDefinition> LevelSelected;
        public static event Action LevelCompleted;
        public static event Action RunWon;
        public static event Action RunLost;
        public static event Action BackToSongSelectRequested;
        public static event Action PauseRequested;
        public static event Action ResumeRequested;

        public static void RaiseTapToStartRequested() => TapToStartRequested?.Invoke();
        public static void RaiseRetryRequested() => RetryRequested?.Invoke();

        public static void RaiseCountdownStarted() => CountdownStarted?.Invoke();
        public static void RaiseCountdownTicked(int value) => CountdownTicked?.Invoke(value);
        public static void RaiseCountdownGo() => CountdownGo?.Invoke();
        public static void RaiseGameplayStarted() => GameplayStarted?.Invoke();
        public static void RaiseRunEnded() => RunEnded?.Invoke();
        public static void RaisePlayerDied() => PlayerDied?.Invoke();

        public static void RaiseJumpPerformed() => JumpPerformed?.Invoke();
        public static void RaiseLaneChanged(int laneIndex) => LaneChanged?.Invoke(laneIndex);
        public static void RaiseSlideStarted() => SlideStarted?.Invoke();
        public static void RaiseSlideEnded() => SlideEnded?.Invoke();

        public static void RaiseFastFallStarted() => FastFallStarted?.Invoke();
        public static void RaiseLevelSelected(EchoRun.Level.LevelDefinition level) => LevelSelected?.Invoke(level);
        public static void RaiseLevelCompleted() => LevelCompleted?.Invoke();
        public static void RaiseRunWon() => RunWon?.Invoke();
        public static void RaiseRunLost() => RunLost?.Invoke();
        public static void RaiseBackToSongSelectRequested() => BackToSongSelectRequested?.Invoke();
        public static void RaisePauseRequested() => PauseRequested?.Invoke();
        public static void RaiseResumeRequested() => ResumeRequested?.Invoke();
    }
}