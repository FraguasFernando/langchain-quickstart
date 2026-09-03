using UnityEngine;

namespace MazePrototype
{
    /// <summary>
    /// Minimal first-person walker: WASD to move, mouse to look, Esc to free the cursor.
    ///
    /// The GameObject this sits on is the "body" (it owns the CharacterController and
    /// handles left/right turning). The camera is a CHILD of it and only handles
    /// looking up/down. Splitting it this way means "forward" for movement always
    /// matches where you are looking horizontally, and you cannot walk up a wall by
    /// looking at the ceiling.
    ///
    /// This component is normally created and configured by MazeBootstrap at runtime,
    /// but it also works if you drop it on a GameObject by hand.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class FirstPersonController : MonoBehaviour
    {
        [Header("Movement")]
        [Tooltip("Walking speed in units per second.")]
        public float moveSpeed = 4.5f;

        [Tooltip("Downward acceleration. Flat maze, so this only keeps the controller glued to the floor.")]
        public float gravity = 20f;

        [Header("Look")]
        [Tooltip("Mouse look sensitivity. Higher = twitchier.")]
        public float mouseSensitivity = 2.0f;

        [Tooltip("How far up/down you can look, in degrees.")]
        public float pitchLimit = 89f;

        [Tooltip("Invert vertical mouse look.")]
        public bool invertY = false;

        [Header("Wiring (auto-filled if left empty)")]
        [Tooltip("The child camera. Found automatically in Awake if not set.")]
        public Transform cameraTransform;

        private CharacterController _controller;
        private float _pitch;              // current up/down camera angle in degrees
        private float _verticalVelocity;   // current fall speed
        private bool _cursorLocked;

        private void Awake()
        {
            // RequireComponent guarantees this exists, so no null check is needed.
            _controller = GetComponent<CharacterController>();

            // If nobody handed us a camera, look for one under us. Null-safe: if there is
            // no camera at all we simply do not apply pitch (the game still runs).
            if (cameraTransform == null)
            {
                var cam = GetComponentInChildren<Camera>();
                if (cam != null) cameraTransform = cam.transform;
            }
        }

        private void Start()
        {
            SetCursorLocked(true);
        }

        private void Update()
        {
            HandleCursorToggle();
            HandleLook();
            HandleMove();
        }

        private void HandleCursorToggle()
        {
            // Esc releases the mouse so you can get back to the Editor.
            if (SimpleInput.EscapePressedThisFrame)
                SetCursorLocked(!_cursorLocked);

            // Clicking back inside the game view recaptures it.
            else if (!_cursorLocked && SimpleInput.ClickedThisFrame)
                SetCursorLocked(true);
        }

        private void HandleLook()
        {
            // While the cursor is free the player is presumably using the Editor —
            // don't spin the camera around underneath them.
            if (!_cursorLocked) return;

            Vector2 look = SimpleInput.LookDelta * mouseSensitivity;

            // Horizontal look turns the whole body. No deltaTime here on purpose:
            // mouse delta is already "how far the mouse moved this frame".
            transform.Rotate(Vector3.up, look.x, Space.World);

            // Vertical look only tilts the camera, and is clamped so you cannot backflip.
            _pitch += invertY ? look.y : -look.y;
            _pitch = Mathf.Clamp(_pitch, -pitchLimit, pitchLimit);
            if (cameraTransform != null)
                cameraTransform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

        private void HandleMove()
        {
            Vector2 input = SimpleInput.MoveAxis;

            // Local-space direction -> world space, so W always means "where I'm facing".
            Vector3 move = transform.right * input.x + transform.forward * input.y;

            // Stops diagonal movement being ~1.41x faster than straight movement.
            if (move.sqrMagnitude > 1f) move.Normalize();

            // Keep a small downward push while grounded; otherwise isGrounded flickers
            // and the controller stutters over collider seams.
            if (_controller.isGrounded && _verticalVelocity < 0f)
                _verticalVelocity = -2f;
            else
                _verticalVelocity -= gravity * Time.deltaTime;

            Vector3 velocity = move * moveSpeed;
            velocity.y = _verticalVelocity;

            // CharacterController.Move does the wall collision for us — this is what stops
            // the player walking through the maze.
            _controller.Move(velocity * Time.deltaTime);
        }

        private void SetCursorLocked(bool locked)
        {
            _cursorLocked = locked;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        // If the game window loses focus, hand the mouse back rather than trapping it.
        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus) SetCursorLocked(false);
        }
    }
}
