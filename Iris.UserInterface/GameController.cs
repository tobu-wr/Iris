using SharpDX.XInput;

namespace Iris.UserInterface
{
    internal sealed class GameController
    {
        private readonly Controller _controller = new(UserIndex.One);
        private Gamepad _gamepad;

        internal enum Button
        {
            DPadUp,
            DPadDown,
            DPadLeft,
            DPadRight,
            Start,
            Back,
            LeftShoulder,
            RightShoulder,
            A,
            B,
            X,
            Y
        }

        internal class ButtonEventArgs : EventArgs
        {
            internal Button Button { get; private set; }

            internal ButtonEventArgs(Button button)
            {
                Button = button;
            }
        }

        internal delegate void ButtonEventHandler(object? sender, ButtonEventArgs e);
        internal event ButtonEventHandler? ButtonDown;
        internal event ButtonEventHandler? ButtonUp;

        internal void Update()
        {
            if (!_controller.IsConnected)
                return;

            Gamepad gamepad = _controller.GetState().Gamepad;
            CheckButton(gamepad, GamepadButtonFlags.DPadUp, Button.DPadUp);
            CheckButton(gamepad, GamepadButtonFlags.DPadDown, Button.DPadDown);
            CheckButton(gamepad, GamepadButtonFlags.DPadLeft, Button.DPadLeft);
            CheckButton(gamepad, GamepadButtonFlags.DPadRight, Button.DPadRight);
            CheckButton(gamepad, GamepadButtonFlags.Start, Button.Start);
            CheckButton(gamepad, GamepadButtonFlags.Back, Button.Back);
            CheckButton(gamepad, GamepadButtonFlags.LeftShoulder, Button.LeftShoulder);
            CheckButton(gamepad, GamepadButtonFlags.RightShoulder, Button.RightShoulder);
            CheckButton(gamepad, GamepadButtonFlags.A, Button.A);
            CheckButton(gamepad, GamepadButtonFlags.B, Button.B);
            CheckButton(gamepad, GamepadButtonFlags.X, Button.X);
            CheckButton(gamepad, GamepadButtonFlags.Y, Button.Y);
            _gamepad = gamepad;
        }

        private void CheckButton(Gamepad gamepad, GamepadButtonFlags flag, Button button)
        {
            if (_gamepad.Buttons.HasFlag(flag) != gamepad.Buttons.HasFlag(flag))
            {
                if (gamepad.Buttons.HasFlag(flag))
                    ButtonDown?.Invoke(this, new ButtonEventArgs(button));
                else
                    ButtonUp?.Invoke(this, new ButtonEventArgs(button));
            }
        }
    }
}
