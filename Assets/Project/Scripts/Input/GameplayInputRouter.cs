using EchoRun.Core;
using EchoRun.Input;
using EchoRun.Player;
using UnityEngine;

namespace EchoRun.Gameplay
{
    public sealed class GameplayInputRouter : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour inputReaderBehaviour;
        [SerializeField] private RunnerMotor runnerMotor;
        [SerializeField] private GameManager gameManager;

        private IInputReader _inputReader;

        private void Awake()
        {
            _inputReader = inputReaderBehaviour as IInputReader;

            if (_inputReader == null)
            {
                Debug.LogError($"{nameof(GameplayInputRouter)} requires a component implementing {nameof(IInputReader)}.", this);
            }
        }

        private void OnEnable()
        {
            if (_inputReader == null)
                return;

            _inputReader.TapPressed += HandleTapPressed;
            _inputReader.SwipedLeft += HandleSwipedLeft;
            _inputReader.SwipedRight += HandleSwipedRight;
            _inputReader.SwipedUp += HandleSwipedUp;
            _inputReader.SwipedDown += HandleSwipedDown;
        }

        private void OnDisable()
        {
            if (_inputReader == null)
                return;

            _inputReader.TapPressed -= HandleTapPressed;
            _inputReader.SwipedLeft -= HandleSwipedLeft;
            _inputReader.SwipedRight -= HandleSwipedRight;
            _inputReader.SwipedUp -= HandleSwipedUp;
            _inputReader.SwipedDown -= HandleSwipedDown;
        }

        private void HandleTapPressed()
        {
            if (gameManager == null)
                return;

            if (gameManager.CurrentState == GameState.WaitingToStart)
            {
                GameSignals.RaiseTapToStartRequested();
            }
        }

        private bool CanControlPlayer()
        {
            if (gameManager == null)
                return false;

            return gameManager.CurrentState == GameState.Countdown
                || gameManager.CurrentState == GameState.Running;
        }

        private void HandleSwipedLeft()
        {
            if (!CanControlPlayer() || runnerMotor == null)
                return;

            runnerMotor.RequestMoveLeft();
        }

        private void HandleSwipedRight()
        {
            if (!CanControlPlayer() || runnerMotor == null)
                return;

            runnerMotor.RequestMoveRight();
        }

        private void HandleSwipedUp()
        {
            if (!CanControlPlayer() || runnerMotor == null)
                return;

            runnerMotor.RequestJump();
        }

        private void HandleSwipedDown()
        {
            if (!CanControlPlayer() || runnerMotor == null)
                return;

            runnerMotor.RequestSlide();
        }
    }
}