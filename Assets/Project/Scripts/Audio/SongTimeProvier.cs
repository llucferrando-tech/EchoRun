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
    }
}