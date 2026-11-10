using BarracksAndBolts.Gameplay.CameraControl;
using BarracksAndBolts.Gameplay.Commands;
using BarracksAndBolts.Gameplay.Input;
using UnityEngine;
using UnityEngine.EventSystems;
using VContainer.Unity;

namespace BarracksAndBolts.Gameplay.Selection
{
    /// <summary>
    /// Left click selects what is under the cursor; left drag selects with a box. The decision is made on release,
    /// because a press can still turn into a drag. The Gameplay map is off while placing,
    /// so the click that places a building never selects anything.
    /// Post-tick for the same reason as placement: the camera has already moved this frame.
    /// </summary>
    public sealed class SelectionInputHandler : IPostTickable
    {
        // A small slip of the mouse during a click must not become a box.
        private const float DragThresholdPixels = 8f;

        private readonly SelectionService _selection;
        private readonly AttackMoveTargeting _targeting;
        private readonly CameraController _cameraController;
        private readonly GameInputActions.GameplayActions _actions;

        private bool _isPressing;
        private bool _isDragging;
        private Vector2 _pressScreenPosition;
        private Vector2 _pressWorldPosition;

        public SelectionInputHandler(SelectionService selection, AttackMoveTargeting targeting, CameraController cameraController,
            GameInputActions inputActions)
        {
            _selection = selection;
            _targeting = targeting;
            _cameraController = cameraController;
            _actions = inputActions.Gameplay;
        }

        public void PostTick()
        {
            // The map can turn off in the middle of a press when placement starts.
            if (!_actions.enabled)
            {
                CancelPress();
                return;
            }

            var pointer = _actions.PointerPosition.ReadValue<Vector2>();
            var pointerWorld = _cameraController.ScreenToWorld(pointer);

            if (_actions.Select.WasPressedThisFrame())
                BeginPress(pointer, pointerWorld);

            if (!_isPressing)
                return;

            if (_actions.Select.IsPressed())
                UpdateDrag(pointer, pointerWorld);
            else
                EndPress(pointerWorld);
        }

        private void BeginPress(Vector2 pointer, Vector2 pointerWorld)
        {
            // A press on a panel belongs to the UI and must not change the selection behind it.
            // While Attack Move waits for its point, the left click is that point.
            if (_targeting.IsUsingClicks || (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()))
                return;

            _isPressing = true;
            _isDragging = false;
            _pressScreenPosition = pointer;

            // Kept in world space, so the box stays on the ground while the camera moves.
            _pressWorldPosition = pointerWorld;
        }

        private void UpdateDrag(Vector2 pointer, Vector2 pointerWorld)
        {
            if (!_isDragging && (pointer - _pressScreenPosition).sqrMagnitude < DragThresholdPixels * DragThresholdPixels)
                return;

            _isDragging = true;
            _selection.UpdateBox(_pressWorldPosition, pointerWorld);
        }

        private void EndPress(Vector2 pointerWorld)
        {
            if (_isDragging)
                _selection.ConfirmBox();
            else
                _selection.SelectAt(pointerWorld);

            _isPressing = false;
            _isDragging = false;
        }

        private void CancelPress()
        {
            if (_isDragging)
                _selection.CancelBox();

            _isPressing = false;
            _isDragging = false;
        }
    }
}
