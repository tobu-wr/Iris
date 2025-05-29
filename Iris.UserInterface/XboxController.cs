using SharpDX.XInput;
using System.Diagnostics;

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

            LeftThumbCenter = GamepadButtonFlags.LeftThumb,
            RightThumbCenter = GamepadButtonFlags.RightThumb,

            LeftShoulder = GamepadButtonFlags.LeftShoulder,
            RightShoulder = GamepadButtonFlags.RightShoulder,

            A = GamepadButtonFlags.A,
            B = GamepadButtonFlags.B,
            X = GamepadButtonFlags.X,
            Y = GamepadButtonFlags.Y,

            LeftTrigger,
            RightTrigger,

            LeftThumbUp,
            LeftThumbDown,
            LeftThumbLeft,
            LeftThumbRight,

            RightThumbUp,
            RightThumbDown,
            RightThumbLeft,
            RightThumbRight
        }

        internal delegate void ButtonEvent_Delegate(Button Button);

        private readonly ButtonEvent_Delegate _buttonDownCallback = buttonDownCallback;
        private readonly ButtonEvent_Delegate _buttonUpCallback = buttonUpCallback;

        private readonly Stopwatch _pollingRateLimiterStopwatch = Stopwatch.StartNew();

        private readonly Controller _xinputController = new(UserIndex.One);
        private State _previousState = new();

        internal void PollInput()
        {
            const long PollingRateLimit = 125;
            long pollingPeriodLimit = Stopwatch.Frequency / PollingRateLimit;

            if (_pollingRateLimiterStopwatch.ElapsedTicks < pollingPeriodLimit)
                return;

            _pollingRateLimiterStopwatch.Restart();

            // Calling GetState is costly, therefore the polling rate has to be limited otherwise
            // some games that call it too frequently like OpenLara can't even run at full speed.
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

            CheckButtonState(GamepadButtonFlags.LeftThumb);
            CheckButtonState(GamepadButtonFlags.RightThumb);

            CheckButtonState(GamepadButtonFlags.LeftShoulder);
            CheckButtonState(GamepadButtonFlags.RightShoulder);

            CheckButtonState(GamepadButtonFlags.A);
            CheckButtonState(GamepadButtonFlags.B);
            CheckButtonState(GamepadButtonFlags.X);
            CheckButtonState(GamepadButtonFlags.Y);

            // ------------------------
            // ------- Triggers -------
            // ------------------------

            bool currentLeftTriggerState = currentState.Gamepad.LeftTrigger >= Gamepad.TriggerThreshold;
            bool previousLeftTriggerState = _previousState.Gamepad.LeftTrigger >= Gamepad.TriggerThreshold;

            if (currentLeftTriggerState != previousLeftTriggerState)
            {
                if (currentLeftTriggerState)
                    _buttonDownCallback(Button.LeftTrigger);
                else
                    _buttonUpCallback(Button.LeftTrigger);
            }

            bool currentRightTriggerState = currentState.Gamepad.RightTrigger >= Gamepad.TriggerThreshold;
            bool previousRightTriggerState = _previousState.Gamepad.RightTrigger >= Gamepad.TriggerThreshold;

            if (currentRightTriggerState != previousRightTriggerState)
            {
                if (currentRightTriggerState)
                    _buttonDownCallback(Button.RightTrigger);
                else
                    _buttonUpCallback(Button.RightTrigger);
            }

            // --------------------------
            // ------- Left Thumb -------
            // --------------------------

            bool currentLeftThumbUpState = currentState.Gamepad.LeftThumbY >= Gamepad.LeftThumbDeadZone;
            bool previousLeftThumbUpState = _previousState.Gamepad.LeftThumbY >= Gamepad.LeftThumbDeadZone;

            if (currentLeftThumbUpState != previousLeftThumbUpState)
            {
                if (currentLeftThumbUpState)
                    _buttonDownCallback(Button.LeftThumbUp);
                else
                    _buttonUpCallback(Button.LeftThumbUp);
            }

            bool currentLeftThumbDownState = currentState.Gamepad.LeftThumbY <= -Gamepad.LeftThumbDeadZone;
            bool previousLeftThumbDownState = _previousState.Gamepad.LeftThumbY <= -Gamepad.LeftThumbDeadZone;

            if (currentLeftThumbDownState != previousLeftThumbDownState)
            {
                if (currentLeftThumbDownState)
                    _buttonDownCallback(Button.LeftThumbDown);
                else
                    _buttonUpCallback(Button.LeftThumbDown);
            }

            bool currentLeftThumbLeftState = currentState.Gamepad.LeftThumbX <= -Gamepad.LeftThumbDeadZone;
            bool previousLeftThumbLeftState = _previousState.Gamepad.LeftThumbX <= -Gamepad.LeftThumbDeadZone;

            if (currentLeftThumbLeftState != previousLeftThumbLeftState)
            {
                if (currentLeftThumbLeftState)
                    _buttonDownCallback(Button.LeftThumbLeft);
                else
                    _buttonUpCallback(Button.LeftThumbLeft);
            }

            bool currentLeftThumbRightState = currentState.Gamepad.LeftThumbX >= Gamepad.LeftThumbDeadZone;
            bool previousLeftThumbRightState = _previousState.Gamepad.LeftThumbX >= Gamepad.LeftThumbDeadZone;

            if (currentLeftThumbRightState != previousLeftThumbRightState)
            {
                if (currentLeftThumbRightState)
                    _buttonDownCallback(Button.LeftThumbRight);
                else
                    _buttonUpCallback(Button.LeftThumbRight);
            }

            // ---------------------------
            // ------- Right Thumb -------
            // ---------------------------

            bool currentRightThumbUpState = currentState.Gamepad.RightThumbY >= Gamepad.RightThumbDeadZone;
            bool previousRightThumbUpState = _previousState.Gamepad.RightThumbY >= Gamepad.RightThumbDeadZone;

            if (currentRightThumbUpState != previousRightThumbUpState)
            {
                if (currentRightThumbUpState)
                    _buttonDownCallback(Button.RightThumbUp);
                else
                    _buttonUpCallback(Button.RightThumbUp);
            }

            bool currentRightThumbDownState = currentState.Gamepad.RightThumbY <= -Gamepad.RightThumbDeadZone;
            bool previousRightThumbDownState = _previousState.Gamepad.RightThumbY <= -Gamepad.RightThumbDeadZone;

            if (currentRightThumbDownState != previousRightThumbDownState)
            {
                if (currentRightThumbDownState)
                    _buttonDownCallback(Button.RightThumbDown);
                else
                    _buttonUpCallback(Button.RightThumbDown);
            }

            bool currentRightThumbLeftState = currentState.Gamepad.RightThumbX <= -Gamepad.RightThumbDeadZone;
            bool previousRightThumbLeftState = _previousState.Gamepad.RightThumbX <= -Gamepad.RightThumbDeadZone;

            if (currentRightThumbLeftState != previousRightThumbLeftState)
            {
                if (currentRightThumbLeftState)
                    _buttonDownCallback(Button.RightThumbLeft);
                else
                    _buttonUpCallback(Button.RightThumbLeft);
            }

            bool currentRightThumbRightState = currentState.Gamepad.RightThumbX >= Gamepad.RightThumbDeadZone;
            bool previousRightThumbRightState = _previousState.Gamepad.RightThumbX >= Gamepad.RightThumbDeadZone;

            if (currentRightThumbRightState != previousRightThumbRightState)
            {
                if (currentRightThumbRightState)
                    _buttonDownCallback(Button.RightThumbRight);
                else
                    _buttonUpCallback(Button.RightThumbRight);
            }

            _previousState = currentState;
        }
    }
}
