using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Gameplay.Buildings;
using BarracksAndBolts.Gameplay.Entities;
using BarracksAndBolts.Gameplay.Teams;
using BarracksAndBolts.Gameplay.Units;
using UnityEngine;

namespace BarracksAndBolts.Gameplay.Hover
{
    /// <summary>What the mouse is over. Picks the same way a click does: the unit in front first, then the building.</summary>
    public sealed class HoverService
    {
        private readonly UnitService _units;
        private readonly BuildingService _buildings;
        private readonly IEventBus _eventBus;

        public HoverService(UnitService units, BuildingService buildings, IEventBus eventBus)
        {
            _units = units;
            _buildings = buildings;
            _eventBus = eventBus;
        }

        public GameEntityId Hovered { get; private set; }

        public void UpdateAt(Vector2 worldPosition)
        {
            var unit = _units.GetUnitAt(worldPosition);
            SetHovered(unit.IsValid ? unit : _buildings.GetBuildingAt(worldPosition));
        }

        public void Clear()
        {
            SetHovered(GameEntityId.None);
        }

        /// <returns>False if nothing is under the mouse or the entity no longer exists.</returns>
        public bool TryGetInfo(out HoverInfo info)
        {
            if (_units.TryGet(Hovered, out var unit))
            {
                info = new HoverInfo(unit.Definition.DisplayName, unit.Faction, unit.Body);
                return true;
            }

            if (_buildings.TryGet(Hovered, out var building))
            {
                info = new HoverInfo(building.Definition.DisplayName, building.Faction, _buildings.GetVisibleArea(building));
                return true;
            }

            info = default;
            return false;
        }

        private void SetHovered(GameEntityId id)
        {
            if (Hovered == id)
                return;

            Hovered = id;
            _eventBus.Publish(new HoverChangedEvent());
        }
    }

    public readonly struct HoverInfo
    {
        public HoverInfo(string name, Faction faction, Rect bounds)
        {
            Name = name;
            Faction = faction;
            Bounds = bounds;
        }

        public string Name { get; }
        public Faction Faction { get; }

        /// <summary>World-space area of the entity, for the highlight frame.</summary>
        public Rect Bounds { get; }
    }

    /// <summary>The mouse moved onto something else. Read the new state from <see cref="HoverService"/>.</summary>
    public readonly struct HoverChangedEvent : IGameEvent
    {
    }
}
