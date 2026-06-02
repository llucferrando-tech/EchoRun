using EchoRun.Core;
using UnityEngine;

namespace EchoRun.UI
{
    public sealed class ContinueButton : MonoBehaviour
    {
        public void WatchAdToContinue()
        {
            GameSignals.RaiseContinueRunRequested();
        }
    }
}