using EchoRun.Core;
using UnityEngine;

namespace EchoRun.UI
{
    public sealed class TapToStartView : MonoBehaviour
    {
        [SerializeField] private GameObject root;

        private void OnEnable()
        {
            GameSignals.CountdownStarted += Hide;
        }

        private void OnDisable()
        {
            GameSignals.CountdownStarted -= Hide;
        }

        private void Start()
        {
            //Show();
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