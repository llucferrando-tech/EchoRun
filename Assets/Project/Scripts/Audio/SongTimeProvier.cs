using UnityEngine;

namespace EchoRun.Audio
{
    public sealed class SongTimeProvider : MonoBehaviour
    {
        [SerializeField] private AudioSource musicSource;

        public bool HasValidSource => musicSource != null;

        public float GetSongTime()
        {
            if (musicSource == null)
                return 0f;

            return musicSource.time;
        }

        public bool IsPlaying()
        {
            return musicSource != null && musicSource.isPlaying;
        }

        public void Play()
        {
            if (musicSource == null)
                return;

            musicSource.Play();
        }

        public void Pause()
        {
            if (musicSource == null)
                return;

            musicSource.Pause();
        }

        public void Stop()
        {
            if (musicSource == null)
                return;

            musicSource.Stop();
        }

        public void SetSongTime(float time)
        {
            if (musicSource == null)
                return;

            if (musicSource.clip != null)
                musicSource.time = Mathf.Clamp(time, 0f, musicSource.clip.length);
            else
                musicSource.time = Mathf.Max(0f, time);
        }
    }
}