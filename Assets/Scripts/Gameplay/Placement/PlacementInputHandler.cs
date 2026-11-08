using BarracksAndBolts.Gameplay.CameraControl;
using BarracksAndBolts.Gameplay.Input;
using UnityEngine;
using UnityEngine.EventSystems;
using VContainer.Unity;

namespace BarracksAndBolts.Gameplay.Placement
{
    /// <summary>
    /// Turns placement input into <see cref="PlacementService"/> calls.
    /// Runs as post-tick, after every MonoBehaviour Update, so the camera has already moved this frame
    /// and the footprint doesn't trail one frame behind while panning.
    /// </summary>
    public sealed class PlacementInputHandler : IPostTickable
    {
        private readonly PlacementService _placement;
        private readonly CameraController _cameraController;
        private readonly GameInputActions.PlacementActions _actions;

        public PlacementInputHandler(PlacementService placement, CameraController cameraController, GameInputActions inputActions)
        {
            _placement = placement;
            _cameraController = cameraController;
            _actions = inputActions.Placement;
        }

        public void PostTick()
        {
            if (!_placement.IsActive)
                return;

            if (_actions.Cancel.WasPressedThisFrame())
            {
                _placement.Cancel();
                return;
            }

            // Hidden over UI, so a click on the production menu can't place a building behind it.
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                _placement.HideArea();
                return;
            }

            var pointer = _actions.PointerPosition.ReadValue<Vector2>();
            _placement.MoveTo(_cameraController.ScreenToWorld(pointer));

            if (_actions.Confirm.WasPressedThisFrame())
                _placement.TryConfirm(keepPlacing: _actions.ConfirmAndContinue.IsPressed());
        }
    }
}
