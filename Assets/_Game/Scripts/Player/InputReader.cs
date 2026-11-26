using UnityEngine;
using UnityEngine.InputSystem;

namespace RunChildRun
{
    /// <summary>
    /// Combines the on-screen joystick / Dash button with keyboard and gamepad (Editor testing).
    /// </summary>
    public sealed class InputReader : MonoBehaviour
    {
        [SerializeField] FloatingJoystick joystick;

        bool dashQueued;

        public Vector2 Move { get; private set; }

        public FloatingJoystick Joystick => joystick;

        public void QueueDash() => dashQueued = true;

        public bool ConsumeDash()
        {
            var d = dashQueued;
            dashQueued = false;
            return d;
        }

        void Update()
        {
            var move = joystick != null ? joystick.Value : Vector2.zero;

            var kb = Keyboard.current;
            if (kb != null)
            {
                var k = Vector2.zero;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) k.x -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) k.x += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) k.y -= 1f;
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) k.y += 1f;
                if (k.sqrMagnitude > move.sqrMagnitude) move = k.normalized;
                if (kb.spaceKey.wasPressedThisFrame || kb.leftShiftKey.wasPressedThisFrame) dashQueued = true;
            }

            var pad = Gamepad.current;
            if (pad != null)
            {
                var s = pad.leftStick.ReadValue();
                if (s.sqrMagnitude > move.sqrMagnitude && s.sqrMagnitude > 0.01f) move = Vector2.ClampMagnitude(s, 1f);
                if (pad.buttonSouth.wasPressedThisFrame || pad.rightTrigger.wasPressedThisFrame) dashQueued = true;
            }

            Move = Vector2.ClampMagnitude(move, 1f);
        }
    }
}
