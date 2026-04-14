using UnityEngine;
using EchoRun.Core;
using Unity.VisualScripting;
using MoreMountains.Feedbacks;

namespace EchoRun.Level
{
    public sealed class LevelMusicController : MonoBehaviour
    {
        [SerializeField] private LevelSession levelSession;
        //[SerializeField] private AudioSource musicSource;
        [SerializeField] private MMF_Player levelSong;
        private MMF_MMSoundManagerSound soundSource;

        private void Awake()
        {
            if (levelSession == null)
            {
                levelSession = FindFirstObjectByType<LevelSession>();
            }
        }

        private void Start()
        {
            if (levelSession != null)
            {
                ApplyLevel(levelSession.CurrentLevel);
            }
            soundSource = levelSong.GetFeedbackOfType<MMF_MMSoundManagerSound>();
        }

        private void OnEnable()
        {
            if (levelSession != null)
            {
                levelSession.LevelChanged += ApplyLevel;
            }

            GameSignals.CountdownStarted += HandleCountdownStarted;
            GameSignals.RunEnded += HandleRunEnded;
        }

        private void OnDisable()
        {
            if (levelSession != null)
            {
                levelSession.LevelChanged -= ApplyLevel;
            }

            GameSignals.CountdownStarted -= HandleCountdownStarted;
            GameSignals.RunEnded -= HandleRunEnded;
        }

        private void ApplyLevel(LevelDefinition level)
        {
            if (soundSource == null)
                return;

            levelSong.StopFeedbacks();
            soundSource.Sfx = level != null ? level.SongClip : null;
        }

        private void HandleCountdownStarted()
        {
            if (soundSource == null || soundSource.Sfx == null)
                return;

            levelSong.StopFeedbacks();
        }

        private void HandleRunEnded()
        {
            if (soundSource == null)
                return;

            levelSong.StopFeedbacks();
        }
    }
}