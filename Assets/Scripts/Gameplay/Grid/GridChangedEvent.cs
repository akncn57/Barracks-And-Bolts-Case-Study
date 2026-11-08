using BarracksAndBolts.Core.Events;
using UnityEngine;

namespace BarracksAndBolts.Gameplay.Grid
{
    /// <summary>Cells inside the area became blocked or free. Paths through it may need to be found again.</summary>
    public readonly struct GridChangedEvent : IGameEvent
    {
        public GridChangedEvent(RectInt area)
        {
            Area = area;
        }

        public RectInt Area { get; }
    }
}
