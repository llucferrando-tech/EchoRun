using System;

namespace EchoRun.Input
{
    public interface IInputReader
    {
        event Action TapPressed;
        event Action SwipedLeft;
        event Action SwipedRight;
        event Action SwipedUp;
    }
}