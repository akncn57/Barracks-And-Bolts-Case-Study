using BarracksAndBolts.Gameplay.Input;
using BarracksAndBolts.Gameplay.Selection;
using VContainer.Unity;

namespace BarracksAndBolts.UI.Common
{
    /// <summary>
    /// Each Esc press closes only the top layer: first the selection, then the open panel.
    /// Canceling a placement comes before these and is handled by the Placement map, which is on alone during placement.
    /// </summary>
    public sealed class EscapeInputHandler : IPostTickable
    {
        private readonly SelectionService _selection;
        private readonly ICloseablePanel _panel;
        private readonly GameInputActions.GameplayActions _actions;

        public EscapeInputHandler(SelectionService selection, ICloseablePanel panel, GameInputActions inputActions)
        {
            _selection = selection;
            _panel = panel;
            _actions = inputActions.Gameplay;
        }

        public void PostTick()
        {
            if (_actions.Cancel.WasPressedThisFrame())
                CloseTopLayer();
        }

        public void CloseTopLayer()
        {
            // The box is still being drawn; its release decides the selection.
            if (_selection.HasBox)
                return;

            if (_selection.HasSelection)
                _selection.Clear();
            else if (_panel.IsOpen)
                _panel.Close();
        }
    }
}
