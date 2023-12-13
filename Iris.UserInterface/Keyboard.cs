using SharpDX.DirectInput;
using System.Diagnostics;

namespace Iris.UserInterface
{
    internal sealed class Keyboard
    {
        internal enum Key
        {
            A = SharpDX.DirectInput.Key.Q,
            Z = SharpDX.DirectInput.Key.W,
            E = SharpDX.DirectInput.Key.E,
            R = SharpDX.DirectInput.Key.R,
            Q = SharpDX.DirectInput.Key.A,
            S = SharpDX.DirectInput.Key.S,
            Return = SharpDX.DirectInput.Key.Return,
            Space = SharpDX.DirectInput.Key.Space,
            Up = SharpDX.DirectInput.Key.Up,
            Left = SharpDX.DirectInput.Key.Left,
            Right = SharpDX.DirectInput.Key.Right,
            Down = SharpDX.DirectInput.Key.Down
        }

        internal delegate void KeyEvent_Delegate(Key key);

        private readonly KeyEvent_Delegate _keyDownCallback;
        private readonly KeyEvent_Delegate _keyUpCallback;

        private readonly Stopwatch _pollingStopwatch = Stopwatch.StartNew();

        private readonly DirectInput _directInput = new();
        private readonly SharpDX.DirectInput.Keyboard _keyboard;
        private KeyboardState _previousState = new();

        internal Keyboard(KeyEvent_Delegate keyDownCallback, KeyEvent_Delegate keyUpCallback)
        {
            _keyDownCallback = keyDownCallback;
            _keyUpCallback = keyUpCallback;

            _keyboard = new(_directInput);
            _keyboard.Acquire();
        }

        internal void PollInput()
        {
            const long PollingRate = 1000;
            long pollingPeriod = Stopwatch.Frequency / PollingRate;

            if (_pollingStopwatch.ElapsedTicks < pollingPeriod)
                return;

            _pollingStopwatch.Restart();

            _keyboard.Poll();

            KeyboardState currentState = _keyboard.GetCurrentState();

            void CheckKeyState(SharpDX.DirectInput.Key key)
            {
                bool currentKeyState = currentState.IsPressed(key);
                bool previousKeyState = _previousState.IsPressed(key);

                if (currentKeyState != previousKeyState)
                {
                    if (currentKeyState)
                        _keyDownCallback((Key)key);
                    else
                        _keyUpCallback((Key)key);
                }
            }

            CheckKeyState(SharpDX.DirectInput.Key.Q);
            CheckKeyState(SharpDX.DirectInput.Key.W);
            CheckKeyState(SharpDX.DirectInput.Key.E);
            CheckKeyState(SharpDX.DirectInput.Key.R);
            CheckKeyState(SharpDX.DirectInput.Key.A);
            CheckKeyState(SharpDX.DirectInput.Key.S);
            CheckKeyState(SharpDX.DirectInput.Key.Return);
            CheckKeyState(SharpDX.DirectInput.Key.Space);
            CheckKeyState(SharpDX.DirectInput.Key.Up);
            CheckKeyState(SharpDX.DirectInput.Key.Left);
            CheckKeyState(SharpDX.DirectInput.Key.Right);
            CheckKeyState(SharpDX.DirectInput.Key.Down);

            _previousState = currentState;
        }
    }
}
