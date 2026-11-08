using BarracksAndBolts.Gameplay.CameraControl;
using BarracksAndBolts.Gameplay.Input;
using UnityEngine;
using UnityEngine.EventSystems;
using VContainer.Unity;

namespace BarracksAndBolts.Gameplay.Hover
{
    /// <summary>
    /// Feeds the pointer into <see cref="HoverService"/> every frame, also when the mouse is still,
    /// because units and the camera move under it. Off while placing and while the mouse is on a panel.
    /// </summary>
    public sealed class HoverInputHandler : IPostTickable
    {
        private readonly HoverService _hover;
        private readonly CameraController _cameraController;
        private readonly GameInputActions.GameplayActions _actions;

        public HoverInputHandler(HoverService hover, CameraController cameraController, GameInputActions inputActions)
        {
            _hover = hover;
            _cameraController = cameraController;
            _actions = inputActions.Gameplay;
        }

        public void PostTick()
        {
            var isOverUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            if (!_actions.enabled || isOverUi)
            {
                _hover.Clear();
                return;
            }

            _hover.UpdateAt(_cameraController.ScreenToWorld(_actions.PointerPosition.ReadValue<Vector2>()));
        }
    }
}
