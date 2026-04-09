using UnityEngine;
using UnityEngine.UI;
using EchoRun.Core;

namespace EchoRun.UI
{
    public sealed class SettingsOverlayController : MonoBehaviour
    {
        [SerializeField] private Button backButton;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button songSelectButton;

        private void OnEnable()
        {
            if (backButton != null)
                backButton.onClick.AddListener(HandleBack);

            if (retryButton != null)
                retryButton.onClick.AddListener(HandleRetry);

            if (songSelectButton != null)
                songSelectButton.onClick.AddListener(HandleSongSelect);
        }

        private void OnDisable()
        {
            if (backButton != null)
                backButton.onClick.RemoveListener(HandleBack);

            if (retryButton != null)
                retryButton.onClick.RemoveListener(HandleRetry);

            if (songSelectButton != null)
                songSelectButton.onClick.RemoveListener(HandleSongSelect);
        }

        private void HandleBack()
        {
            GameSignals.RaiseResumeRequested();
        }

        private void HandleRetry()
        {
            GameSignals.RaiseRetryRequested();
        }

        private void HandleSongSelect()
        {
            GameSignals.RaiseBackToSongSelectRequested();
        }
    }
}