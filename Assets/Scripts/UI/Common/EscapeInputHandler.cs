using BarracksAndBolts.Gameplay.Input;
using BarracksAndBolts.Gameplay.Selection;
using VContainer.Unity;

namespace BarracksAndBolts.UI.Common
{
    /// <summary>
    /// Each Esc press closes only the top layer: first an open window (settings), then the selection, then the open panel.
    /// With nothing left to close it opens the settings, as most games do.
    /// Canceling a placement comes before these and is handled by the Placement map, which is on alone during placement.
    /// </summary>
    public sealed class EscapeInputHandler : IPostTickable
    {
        private readonly SelectionService _selection;
        private readonly ICloseablePanel _panel;
        private readonly IEscapeMenu _menu;
        private readonly GameInputActions.GameplayActions _actions;

        public EscapeInputHandler(SelectionService selection, ICloseablePanel panel, IEscapeMenu menu, GameInputActions inputActions)
        {
            _selection = selection;
            _panel = panel;
            _menu = menu;
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
            if (_selection.HasBox || _menu.TryCloseTop())
                return;

            if (_selection.HasSelection)
                _selection.Clear();
            else if (_panel.IsOpen)
                _panel.Close();
            else
                _menu.Open();
        }
    }
}
