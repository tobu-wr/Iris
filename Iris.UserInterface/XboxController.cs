﻿using SharpDX.XInput;
using System.Diagnostics;
using static Iris.UserInterface.XboxController;

namespace Iris.UserInterface
{
    internal sealed class XboxController(ButtonEvent_Delegate buttonDownCallback, ButtonEvent_Delegate buttonUpCallback)
    {
        internal enum Button : ushort
        {
            None = 0,
            DPadUp = 1,
            DPadDown = 2,
            DPadLeft = 4,
            DPadRight = 8,
            Start = 0x10,
            Back = 0x20,
            LeftThumb = 0x40,
            RightThumb = 0x80,
            LeftShoulder = 0x100,
            RightShoulder = 0x200,
            A = 0x1000,
            B = 0x2000,
            X = 0x4000,
            Y = 0x8000
        }

        internal delegate void ButtonEvent_Delegate(Button Button);

        private readonly ButtonEvent_Delegate _buttonDownCallback = buttonDownCallback;
        private readonly ButtonEvent_Delegate _buttonUpCallback = buttonUpCallback;

        private readonly Stopwatch _pollingStopwatch = Stopwatch.StartNew();

        private readonly Controller _xinputController = new(UserIndex.One);
        private State _previousState;

        internal void PollInput()
        {
            const long PollingRate = 125;
            long pollingPeriod = Stopwatch.Frequency / PollingRate;

            if (_pollingStopwatch.ElapsedTicks < pollingPeriod)
                return;

            _pollingStopwatch.Restart();

            if (!_xinputController.GetState(out State currentState))
                return;

            if (currentState.PacketNumber == _previousState.PacketNumber)
                return;

            void CheckButtonState(GamepadButtonFlags flag)
            {
                bool currentButtonState = currentState.Gamepad.Buttons.HasFlag(flag);
                bool previousButtonState = _previousState.Gamepad.Buttons.HasFlag(flag);

                if (currentButtonState != previousButtonState)
                {
                    if (currentButtonState)
                        _buttonDownCallback((Button)flag);
                    else
                        _buttonUpCallback((Button)flag);
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
