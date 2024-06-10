using SharpDX.XInput;

namespace Iris.UserInterface
{
    internal sealed class XboxController(XboxController.ButtonEvent_Delegate buttonDownCallback, XboxController.ButtonEvent_Delegate buttonUpCallback)
    {
        internal enum Button
        {
            DPadUp = GamepadButtonFlags.DPadUp,
            DPadDown = GamepadButtonFlags.DPadDown,
            DPadLeft = GamepadButtonFlags.DPadLeft,
            DPadRight = GamepadButtonFlags.DPadRight,
            Start = GamepadButtonFlags.Start,
            Back = GamepadButtonFlags.Back,
            LeftShoulder = GamepadButtonFlags.LeftShoulder,
            RightShoulder = GamepadButtonFlags.RightShoulder,
            A = GamepadButtonFlags.A,
            B = GamepadButtonFlags.B,
            X = GamepadButtonFlags.X,
            Y = GamepadButtonFlags.Y
        }

        internal delegate void ButtonEvent_Delegate(Button Button);

        private readonly ButtonEvent_Delegate _buttonDownCallback = buttonDownCallback;
        private readonly ButtonEvent_Delegate _buttonUpCallback = buttonUpCallback;

        private readonly Controller _xinputController = new(UserIndex.One);
        private State _previousState = new();

        internal void PollInput()
        {
            if (!_xinputController.GetState(out State currentState))
                return;

            if (currentState.PacketNumber == _previousState.PacketNumber)
                return;

            void CheckButtonState(GamepadButtonFlags button)
            {
                bool currentButtonState = currentState.Gamepad.Buttons.HasFlag(button);
                bool previousButtonState = _previousState.Gamepad.Buttons.HasFlag(button);

                if (currentButtonState != previousButtonState)
                {
                    if (currentButtonState)
                        _buttonDownCallback((Button)button);
                    else
                        _buttonUpCallback((Button)button);
                }
            }

            CheckButtonState(GamepadButtonFlags.DPadUp);
            CheckButtonState(GamepadButtonFlags.DPadDown);
            CheckButtonState(GamepadButtonFlags.DPadLeft);
            CheckButtonState(GamepadButtonFlags.DPadRight);
            CheckButtonState(GamepadButtonFlags.Start);
            CheckButtonState(GamepadButtonFlags.Back);
            CheckButtonState(GamepadButtonFlags.LeftShoulder);
            CheckButtonState(GamepadButtonFlags.RightShoulder);
            CheckButtonState(GamepadButtonFlags.A);
            CheckButtonState(GamepadButtonFlags.B);
            CheckButtonState(GamepadButtonFlags.X);
            CheckButtonState(GamepadButtonFlags.Y);

            _previousState = currentState;
        }
    }
}
