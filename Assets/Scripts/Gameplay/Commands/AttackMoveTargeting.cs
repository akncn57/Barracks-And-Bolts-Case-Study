using UnityEngine;

namespace BarracksAndBolts.Gameplay.Commands
{
    /// <summary>
    /// The short mode after the Attack Move key or button: the next left click on the map picks where to go,
    /// a right click or Esc cancels. While it is on, left clicks don't select.
    /// </summary>
    public sealed class AttackMoveTargeting
    {
        private int _endedFrame = -1;

        public bool IsActive { get; private set; }

        /// <summary>True while targeting, and on the frame it ended, so the click that ended it doesn't also select.</summary>
        public bool IsUsingClicks => IsActive || _endedFrame == Time.frameCount;

        public void Begin()
        {
            IsActive = true;
        }

        public void End()
        {
            if (!IsActive)
                return;

            IsActive = false;
            _endedFrame = Time.frameCount;
        }
    }
}
