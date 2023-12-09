using SharpDX.XInput;

namespace Iris.UserInterface
{
    internal sealed class XboxController
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

            void CheckButton(GamepadButtonFlags flag, Button button)
            {
                if (_gamepad.Buttons.HasFlag(flag) != _controller.GetState().Gamepad.Buttons.HasFlag(flag))
                {
                    if (_controller.GetState().Gamepad.Buttons.HasFlag(flag))
                        ButtonDown?.Invoke(this, new ButtonEventArgs(button));
                    else
                        ButtonUp?.Invoke(this, new ButtonEventArgs(button));
                }
            }

            CheckButton(GamepadButtonFlags.DPadUp, Button.DPadUp);
            CheckButton(GamepadButtonFlags.DPadDown, Button.DPadDown);
            CheckButton(GamepadButtonFlags.DPadLeft, Button.DPadLeft);
            CheckButton(GamepadButtonFlags.DPadRight, Button.DPadRight);
            CheckButton(GamepadButtonFlags.Start, Button.Start);
            CheckButton(GamepadButtonFlags.Back, Button.Back);
            CheckButton(GamepadButtonFlags.LeftShoulder, Button.LeftShoulder);
            CheckButton(GamepadButtonFlags.RightShoulder, Button.RightShoulder);
            CheckButton(GamepadButtonFlags.A, Button.A);
            CheckButton(GamepadButtonFlags.B, Button.B);
            CheckButton(GamepadButtonFlags.X, Button.X);
            CheckButton(GamepadButtonFlags.Y, Button.Y);

            _gamepad = _controller.GetState().Gamepad;
        }
    }
}
