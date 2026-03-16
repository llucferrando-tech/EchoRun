using EchoRun.Core;
using UnityEngine;

namespace EchoRun.UI
{
    public sealed class GameOverView : MonoBehaviour
    {
        [SerializeField] private GameObject root;

        private void OnEnable()
        {
            GameSignals.RunStarted += Hide;
            GameSignals.PlayerDied += Show;
        }

        private void OnDisable()
        {
            GameSignals.RunStarted -= Hide;
            GameSignals.PlayerDied -= Show;
        }

        private void Start()
        {
            Hide();
        }

        private void Show()
        {
            if (root != null)
                root.SetActive(true);
        }

        private void Hide()
        {
            if (root != null)
                root.SetActive(false);
        }
    }
}