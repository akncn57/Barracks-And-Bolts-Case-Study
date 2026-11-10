using BarracksAndBolts.Gameplay.Input;
using VContainer.Unity;

namespace BarracksAndBolts.Gameplay.TimeControl
{
    /// <summary>P pauses and resumes, + and - change the game speed. The buttons on the resource bar do the same.</summary>
    public sealed class TimeControlInputHandler : IPostTickable
    {
        private readonly TimeControlService _timeControl;
        private readonly GameInputActions.GameplayActions _actions;

        public TimeControlInputHandler(TimeControlService timeControl, GameInputActions inputActions)
        {
            _timeControl = timeControl;
            _actions = inputActions.Gameplay;
        }

        public void PostTick()
        {
            if (!_actions.enabled)
                return;

            if (_actions.TogglePause.WasPressedThisFrame())
                _timeControl.TogglePause();

            if (_actions.SpeedUp.WasPressedThisFrame())
                _timeControl.SpeedUp();

            if (_actions.SpeedDown.WasPressedThisFrame())
                _timeControl.SpeedDown();
        }
    }
}
