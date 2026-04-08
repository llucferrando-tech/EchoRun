using UnityEngine;
using EchoRun.Core;

namespace EchoRun.UI
{
    public class ScoreView : MonoBehaviour
    {
        [SerializeField] private GameObject root;

        private void OnEnable()
        {
            GameSignals.CountdownStarted += Show;
            GameSignals.RunEnded += Hide;
        }

        private void OnDisable()
        {
            GameSignals.CountdownStarted -= Show;
            GameSignals.RunEnded -= Hide;
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
