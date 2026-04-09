using UnityEngine;
using EchoRun.Core;

namespace EchoRun.UI
{
    public sealed class UIActionRouter : MonoBehaviour
    {
        public void Pause()
        {
            GameSignals.RaisePauseRequested();
        }

        public void Resume()
        {
            GameSignals.RaiseResumeRequested();
        }

        public void Retry()
        {
            GameSignals.RaiseRetryRequested();
            Debug.Log("Retry");
        }

        public void BackToSongSelect()
        {
            GameSignals.RaiseBackToSongSelectRequested();
        }

        public void TapToStart()
        {
            GameSignals.RaiseTapToStartRequested();
        }
    }
}