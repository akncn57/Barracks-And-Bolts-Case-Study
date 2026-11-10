using BarracksAndBolts.Core.Settings;
using BarracksAndBolts.Gameplay.Grid;
using BarracksAndBolts.Gameplay.Input;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;
using VContainer;

namespace BarracksAndBolts.Gameplay.CameraControl
{
    /// <summary>
    /// RTS camera input: keyboard, edge and middle mouse drag panning, zoom towards the cursor, clamped to the map.
    /// Moves this transform (the Cinemachine follow target) and sets the Cinemachine lens size;
    /// Cinemachine then drives the real camera.
    /// Runs on real time instead of the game clock, so the player can look around while the game is paused.
    /// </summary>
    public sealed class CameraController : MonoBehaviour
    {
        [SerializeField] private CameraConfig config;
        [SerializeField] private CinemachineCamera cinemachineCamera;

        private GridMap _gridMap;
        private GameSettings _settings;
        private GameInputActions.CameraActions _actions;
        private Rect _bounds;
        private float _targetZoom;
        private bool _isDragging;

        private float Zoom
        {
            get => cinemachineCamera.Lens.OrthographicSize;
            set => cinemachineCamera.Lens.OrthographicSize = value;
        }

        private static Vector2 ScreenSize => new(Screen.width, Screen.height);

        [Inject]
        private void Construct(GridMap gridMap, GameInputActions inputActions, GameSettings settings)
        {
            _gridMap = gridMap;
            _actions = inputActions.Camera;
            _settings = settings;
        }

        /// <summary>
        /// World point under a screen position. Uses this frame's target and zoom; the real camera's
        /// ScreenToWorldPoint can still hold last frame's values because Cinemachine moves it in LateUpdate.
        /// </summary>
        public Vector2 ScreenToWorld(Vector2 screenPosition)
        {
            return CameraMath.ScreenToWorld(screenPosition, ScreenSize, transform.position, Zoom);
        }

        /// <summary>Screen position of a world point, with the same values as <see cref="ScreenToWorld"/>.</summary>
        public Vector2 WorldToScreen(Vector2 worldPosition)
        {
            return CameraMath.WorldToScreen(worldPosition, ScreenSize, transform.position, Zoom);
        }

        /// <summary>The world point in the middle of the view.</summary>
        public Vector2 Center => transform.position;

        /// <summary>Jumps the view to the point, kept inside the map.</summary>
        public void LookAt(Vector2 worldPoint)
        {
            MoveTo(worldPoint);
        }

        private void Start()
        {
            var padding = Vector2.one * config.BoundsPadding;
            _bounds = new Rect(_gridMap.Origin - padding, new Vector2(_gridMap.Width, _gridMap.Height) + padding * 2f);

            _targetZoom = Mathf.Clamp(Zoom, config.MinZoom, config.MaxZoom);
            Zoom = _targetZoom;
            MoveTo(_bounds.center);

            _actions.Enable();
        }

        private void OnDestroy()
        {
            // The input actions belong to the scope and may already be disposed when the scene unloads.
            if (_actions.enabled)
                _actions.Disable();
        }

        private void Update()
        {
            var deltaTime = Time.unscaledDeltaTime;
            var isPointerOverUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

            var center = UpdateZoom((Vector2)transform.position, deltaTime, isPointerOverUi);
            center += GetPanDirection() * (config.PanSpeed * _settings.CameraSpeedFactor * Zoom * 2f * deltaTime);
            center -= GetDragDelta(isPointerOverUi);

            // Clamped every frame, so a resized window or a zoom change can never show outside the map.
            MoveTo(center);
        }

        private Vector2 UpdateZoom(Vector2 center, float deltaTime, bool isPointerOverUi)
        {
            var scroll = _actions.Zoom.ReadValue<float>();
            if (!Mathf.Approximately(scroll, 0f) && !isPointerOverUi)
            {
                // Scroll values differ between platforms, so only the direction is used: one notch, one step.
                _targetZoom = Mathf.Clamp(_targetZoom - Mathf.Sign(scroll) * config.ZoomStep, config.MinZoom, config.MaxZoom);
            }

            var oldSize = Zoom;
            if (Mathf.Approximately(oldSize, _targetZoom))
                return center;

            // Exponential smoothing; unlike a plain Lerp it behaves the same at any frame rate.
            var blend = 1f - Mathf.Exp(-config.ZoomSharpness * deltaTime);
            var newSize = Mathf.Abs(_targetZoom - oldSize) < 0.01f ? _targetZoom : Mathf.Lerp(oldSize, _targetZoom, blend);

            // Zoom towards the cursor, or towards the center when the cursor is outside the window.
            var pointer = _actions.PointerPosition.ReadValue<Vector2>();
            var isPointerOnScreen = new Rect(Vector2.zero, ScreenSize).Contains(pointer);
            var focus = isPointerOnScreen ? CameraMath.ScreenToWorld(pointer, ScreenSize, center, oldSize) : center;

            Zoom = newSize;
            return CameraMath.ZoomTowards(center, focus, oldSize, newSize);
        }

        private Vector2 GetPanDirection()
        {
            var keyboard = _actions.Pan.ReadValue<Vector2>();
            if (keyboard != Vector2.zero || _isDragging || !config.IsEdgePanEnabled || !_settings.IsEdgePanOn || !Application.isFocused)
                return Vector2.ClampMagnitude(keyboard, 1f);

            return CameraMath.GetEdgePanDirection(_actions.PointerPosition.ReadValue<Vector2>(), ScreenSize, config.EdgeThickness);
        }

        private Vector2 GetDragDelta(bool isPointerOverUi)
        {
            // A drag that starts on a UI panel belongs to the UI, so it must not move the camera.
            if (_actions.DragPan.WasPressedThisFrame())
                _isDragging = !isPointerOverUi;
            else if (!_actions.DragPan.IsPressed())
                _isDragging = false;

            if (!_isDragging)
                return Vector2.zero;

            var unitsPerPixel = CameraMath.GetWorldUnitsPerPixel(Zoom, Screen.height);
            return _actions.PointerDelta.ReadValue<Vector2>() * unitsPerPixel;
        }

        private void MoveTo(Vector2 center)
        {
            var aspect = Screen.height > 0 ? (float)Screen.width / Screen.height : 1f;
            var halfExtents = CameraMath.GetViewHalfExtents(Zoom, aspect);
            var clamped = CameraMath.ClampCenter(center, halfExtents, _bounds);
            transform.position = new Vector3(clamped.x, clamped.y, transform.position.z);
        }
    }
}
