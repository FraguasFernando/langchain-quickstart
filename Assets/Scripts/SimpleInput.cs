using UnityEngine;

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

namespace MazePrototype
{
    /// <summary>
    /// A tiny input wrapper so this prototype works no matter which input backend the
    /// project is set to (Edit > Project Settings > Player > Active Input Handling).
    ///
    /// Why this exists: Unity 6 templates often ship with the NEW Input System, and in that
    /// mode the old <c>Input.GetAxis("Mouse X")</c> calls throw an exception at runtime.
    /// The #if below picks the right API at COMPILE time:
    ///   - ENABLE_INPUT_SYSTEM is defined when the Input System package is installed
    ///   - ENABLE_LEGACY_INPUT_MANAGER is defined when the old Input Manager is enabled
    /// If both are on ("Both"), we use the legacy path because it needs no extra setup.
    /// </summary>
    public static class SimpleInput
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        // The new Input System reports mouse movement in raw pixels per frame, while the old
        // Input Manager pre-scales it. This factor makes both backends feel roughly the same.
        private const float NewInputMouseScale = 0.05f;
#endif

        /// <summary>WASD / arrow keys as a -1..1 vector. x = strafe, y = forward.</summary>
        public static Vector2 MoveAxis
        {
            get
            {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
                var kb = Keyboard.current;
                if (kb == null) return Vector2.zero; // no keyboard attached — do nothing

                float x = 0f, y = 0f;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) x -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) x += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) y -= 1f;
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) y += 1f;
                return new Vector2(x, y);
#else
                // GetAxisRaw gives instant -1/0/1 with no smoothing, which feels crisper
                // for a greybox walk-through.
                return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
#endif
            }
        }

        /// <summary>Mouse movement since last frame, already scaled to a common range.</summary>
        public static Vector2 LookDelta
        {
            get
            {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
                var mouse = Mouse.current;
                if (mouse == null) return Vector2.zero;
                return mouse.delta.ReadValue() * NewInputMouseScale;
#else
                return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
#endif
            }
        }

        /// <summary>True on the single frame Escape is pressed down.</summary>
        public static bool EscapePressedThisFrame
        {
            get
            {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
                var kb = Keyboard.current;
                return kb != null && kb.escapeKey.wasPressedThisFrame;
#else
                return Input.GetKeyDown(KeyCode.Escape);
#endif
            }
        }

        /// <summary>True on the single frame the left mouse button is pressed down.</summary>
        public static bool ClickedThisFrame
        {
            get
            {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
                var mouse = Mouse.current;
                return mouse != null && mouse.leftButton.wasPressedThisFrame;
#else
                return Input.GetMouseButtonDown(0);
#endif
            }
        }
    }
}
