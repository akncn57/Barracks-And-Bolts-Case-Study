using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Gameplay.CameraControl;
using BarracksAndBolts.Gameplay.Feedback;
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
        private readonly IEventBus _eventBus;

        public PlacementInputHandler(PlacementService placement, CameraController cameraController, GameInputActions inputActions, IEventBus eventBus)
        {
            _placement = placement;
            _cameraController = cameraController;
            _actions = inputActions.Placement;
            _eventBus = eventBus;
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

            var pointer = _cameraController.ScreenToWorld(_actions.PointerPosition.ReadValue<Vector2>());
            _placement.MoveTo(pointer);

            if (!_actions.Confirm.WasPressedThisFrame())
                return;

            // Read before confirming: a successful placement ends the mode.
            var block = _placement.Block;
            if (!_placement.TryConfirm(keepPlacing: _actions.ConfirmAndContinue.IsPressed()))
                _eventBus.Publish(new FloatingTextRequestedEvent(GetBlockMessage(block, _placement.IsPlacingUnit), pointer, FloatingTextKind.Warning));
        }

        private static string GetBlockMessage(PlacementBlock block, bool isUnit)
        {
            if (isUnit)
                return block == PlacementBlock.NotEnoughResources ? "Not enough resources" : "Can't place a unit here";

            if (block == PlacementBlock.UnitInTheWay)
                return "Units are in the way";

            switch (block)
            {
                case PlacementBlock.OutsideMap:
                    return "Can't build outside the map";
                case PlacementBlock.OutsideTerritory:
                    return "Build inside your territory";
                case PlacementBlock.UnitInTheWay:
                    return "Units are in the way";
                case PlacementBlock.NotEnoughResources:
                    return "Not enough resources";
                default:
                    return "Can't build here";
            }
        }
    }
}
