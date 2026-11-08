using System;
using System.Collections.Generic;
using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Core.Simulation;
using BarracksAndBolts.Gameplay.Grid;
using BarracksAndBolts.Gameplay.Pathfinding;
using BarracksAndBolts.Utils.Disposables;
using UnityEngine;

namespace BarracksAndBolts.Gameplay.Units
{
    /// <summary>
    /// Walks units along their paths every simulation tick, and finds a new path for a unit
    /// when a new building lands on the path it was following.
    /// </summary>
    public sealed class UnitMovementSystem : ISimulationSystem, IDisposable
    {
        private readonly UnitService _units;
        private readonly GridMap _gridMap;
        private readonly AStarPathfinder _pathfinder;
        private readonly CompositeDisposable _subscriptions = new();
        private readonly List<Vector2Int> _cellPath = new();
        private readonly List<Vector2> _worldPath = new();
        private readonly Func<Vector2, Vector2Int> _worldToCell;

        public UnitMovementSystem(UnitService units, GridMap gridMap, AStarPathfinder pathfinder, IEventBus eventBus)
        {
            _units = units;
            _gridMap = gridMap;
            _pathfinder = pathfinder;
            _worldToCell = gridMap.WorldToCell;
            eventBus.Subscribe<GridChangedEvent>(OnGridChanged).AddTo(_subscriptions);
        }

        public SimulationPhase Phase => SimulationPhase.Movement;

        public void Tick(float deltaTime)
        {
            foreach (var unit in _units.Units)
            {
                if (unit.IsMoving)
                    unit.MoveAlongPath(unit.Definition.MoveSpeed * deltaTime);
            }
        }

        /// <summary>Sends the unit to the cell, or as close as it can get if the cell can't be reached.</summary>
        public void SendTo(UnitModel unit, Vector2Int cell)
        {
            var status = _pathfinder.FindPath(_gridMap.WorldToCell(unit.Position), cell, _cellPath);
            if (status == PathStatus.NotFound)
            {
                unit.Stop();
                return;
            }

            _worldPath.Clear();
            foreach (var pathCell in _cellPath)
                _worldPath.Add(_gridMap.CellToWorldCenter(pathCell));

            // Already on the goal cell but maybe not on its center, e.g. stopped halfway by a new order.
            if (_worldPath.Count == 0 && status == PathStatus.Found)
                _worldPath.Add(_gridMap.CellToWorldCenter(cell));

            unit.SetPath(_worldPath);
        }

        public void Dispose()
        {
            _subscriptions.Dispose();
        }

        private void OnGridChanged(GridChangedEvent changedEvent)
        {
            foreach (var unit in _units.Units)
            {
                if (unit.IsMoving && unit.IsPathThrough(changedEvent.Area, _worldToCell))
                    SendTo(unit, _gridMap.WorldToCell(unit.Destination));
            }
        }
    }
}
