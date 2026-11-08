using BarracksAndBolts.Gameplay.CameraControl;
using BarracksAndBolts.Gameplay.Input;
using BarracksAndBolts.Gameplay.Selection;
using UnityEngine;
using UnityEngine.EventSystems;
using VContainer.Unity;

namespace BarracksAndBolts.Gameplay.Commands
{
    /// <summary>
    /// Right click orders the selected units. The Gameplay map is off while placing,
    /// so the right click that cancels placement never moves anyone.
    /// </summary>
    public sealed class CommandInputHandler : IPostTickable
    {
        private readonly UnitCommandService _commands;
        private readonly SelectionService _selection;
        private readonly CameraController _cameraController;
        private readonly GameInputActions.GameplayActions _actions;

        public CommandInputHandler(UnitCommandService commands, SelectionService selection, CameraController cameraController, GameInputActions inputActions)
        {
            _commands = commands;
            _selection = selection;
            _cameraController = cameraController;
            _actions = inputActions.Gameplay;
        }

        public void PostTick()
        {
            if (!_actions.Command.WasPressedThisFrame() || !_selection.HasSelection)
                return;

            // A click on a panel belongs to the UI and must not send units behind it.
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            var pointer = _actions.PointerPosition.ReadValue<Vector2>();
            _commands.CommandAt(_selection.Selected, _cameraController.ScreenToWorld(pointer));
        }
    }
}
