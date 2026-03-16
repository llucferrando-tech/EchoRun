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
        }

        private void OnDisable()
        {
            if (_inputReader == null)
                return;

            _inputReader.TapPressed -= HandleTapPressed;
            _inputReader.SwipedLeft -= HandleSwipedLeft;
            _inputReader.SwipedRight -= HandleSwipedRight;
            _inputReader.SwipedUp -= HandleSwipedUp;
        }

        private void HandleTapPressed()
        {
            if (gameManager.CurrentState == GameState.WaitingToStart)
            {
                GameSignals.RaiseTapToStartRequested();
                return;
            }

            if (gameManager.CurrentState == GameState.GameOver)
            {
                GameSignals.RaiseRetryRequested();
            }
        }

        private void HandleSwipedLeft()
        {
            if (gameManager.CurrentState != GameState.Running)
                return;

            runnerMotor.RequestMoveLeft();
        }

        private void HandleSwipedRight()
        {
            if (gameManager.CurrentState != GameState.Running)
                return;

            runnerMotor.RequestMoveRight();
        }

        private void HandleSwipedUp()
        {
            if (gameManager.CurrentState != GameState.Running)
                return;

            runnerMotor.RequestJump();
        }
    }
}