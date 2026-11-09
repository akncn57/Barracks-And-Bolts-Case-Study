using BarracksAndBolts.Gameplay.Buildings;
using BarracksAndBolts.Gameplay.Sandbox;
using UnityEngine;
using VContainer;

namespace BarracksAndBolts.Gameplay.Hover
{
    /// <summary>
    /// Turns the mouse into a tool over work for villagers: an axe or pickaxe over resources, a hammer over the player's
    /// building sites. Only in a normal game; sandbox mode has no villagers.
    /// </summary>
    public sealed class ToolCursorView : MonoBehaviour
    {
        [Tooltip("Textures imported as Cursor. Empty slots keep the normal cursor.")]
        [SerializeField] private Texture2D axeCursor;
        [SerializeField] private Texture2D pickaxeCursor;
        [SerializeField] private Texture2D hammerCursor;
        [Tooltip("The point of the texture that clicks, in pixels from its top-left corner.")]
        [SerializeField] private Vector2 hotspot = new(16f, 16f);

        private HoverService _hover;
        private BuildingService _buildings;
        private bool _isEnabled;
        private ToolCursor _shown;

        [Inject]
        private void Construct(HoverService hover, BuildingService buildings, SandboxService sandbox)
        {
            _hover = hover;
            _buildings = buildings;
            _isEnabled = !sandbox.IsEnabled;
        }

        // LateUpdate, so the hover is already up to date this frame.
        private void LateUpdate()
        {
            if (!_isEnabled)
                return;

            var building = _buildings.TryGet(_hover.Hovered, out var hovered) ? hovered : null;
            var tool = ToolCursorPicker.Pick(_hover.HoveredNode, building);
            if (tool != _shown)
                Show(tool);
        }

        // The cursor belongs to the whole application; leaving the scene must not leave a tool behind.
        private void OnDisable()
        {
            if (_shown != ToolCursor.None)
                Show(ToolCursor.None);
        }

        private void Show(ToolCursor tool)
        {
            _shown = tool;
            var texture = GetTexture(tool);
            Cursor.SetCursor(texture, texture != null ? hotspot : Vector2.zero, CursorMode.Auto);
        }

        private Texture2D GetTexture(ToolCursor tool)
        {
            switch (tool)
            {
                case ToolCursor.Axe:
                    return axeCursor;
                case ToolCursor.Pickaxe:
                    return pickaxeCursor;
                case ToolCursor.Hammer:
                    return hammerCursor;
                default:
                    return null;
            }
        }
    }
}
