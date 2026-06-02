using EchoRun.Core;
using UnityEngine;

namespace EchoRun.UI
{
    public sealed class MainMenuController : MonoBehaviour
    {
        public void StartGame()
        {
            GameSignals.RaiseMainMenuStartRequested();
            Debug.Log("Main menu start requested.");
        }
        
    }
}