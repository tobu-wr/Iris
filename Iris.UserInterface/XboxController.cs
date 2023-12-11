using SharpDX.XInput;

namespace Iris.UserInterface
{
    internal sealed class XboxController
    {
        private readonly Controller _xinputController = new(UserIndex.One);
        private GamepadButtonFlags _previousButtonStates;

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

        internal class ButtonEventArgs(Button button) : EventArgs
        {
            internal Button Button { get; private set; } = button;
        }

        internal delegate void ButtonEventHandler(object? sender, ButtonEventArgs e);
        internal event ButtonEventHandler? ButtonDown;
        internal event ButtonEventHandler? ButtonUp;

        internal void Poll()
        {
            if (!_xinputController.IsConnected)
                return;

            GamepadButtonFlags currentButtonStates = _xinputController.GetState().Gamepad.Buttons;

            void CheckButton(GamepadButtonFlags flag, Button button)
            {
                bool previousState = _previousButtonStates.HasFlag(flag);
                bool currentState = currentButtonStates.HasFlag(flag);

                if (previousState != currentState)
                {
                    if (currentState)
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

            _previousButtonStates = currentButtonStates;
        }
    }
}
