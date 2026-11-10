using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Gameplay.Buildings;
using BarracksAndBolts.Gameplay.CameraControl;
using BarracksAndBolts.Gameplay.Data;
using BarracksAndBolts.Gameplay.Feedback;
using BarracksAndBolts.Gameplay.Grid;
using BarracksAndBolts.Gameplay.Input;
using BarracksAndBolts.Gameplay.Selection;
using BarracksAndBolts.Gameplay.Teams;
using BarracksAndBolts.Gameplay.Units;
using UnityEngine;
using UnityEngine.InputSystem.Controls;
using VContainer.Unity;

namespace BarracksAndBolts.Gameplay.Commands
{
    /// <summary>
    /// The keyboard shortcuts of the Gameplay map: idle villager (.), control groups (Ctrl+1-9, 1-9, twice to look),
    /// Attack Move (F), Stop (X), Hold Position (H) and the Castle (Home). The command buttons on the unit card call the
    /// same methods, so a key and its button always do the same thing.
    /// </summary>
    public sealed class HotkeyInputHandler : IPostTickable
    {
        // A second press of the same group number within this time moves the camera to the group.
        private const float DoubleTapSeconds = 0.35f;

        private readonly SelectionService _selection;
        private readonly UnitCommandService _commands;
        private readonly AttackMoveTargeting _targeting;
        private readonly IdleVillagerFinder _idleVillagers;
        private readonly ControlGroupService _groups;
        private readonly UnitService _units;
        private readonly BuildingService _buildings;
        private readonly NormalModeSettings _normalMode;
        private readonly GridMap _gridMap;
        private readonly CameraController _camera;
        private readonly IEventBus _eventBus;
        private readonly GameInputActions.GameplayActions _actions;

        private int _lastGroup;
        private float _lastGroupTime = float.NegativeInfinity;

        public HotkeyInputHandler(SelectionService selection, UnitCommandService commands, AttackMoveTargeting targeting,
            IdleVillagerFinder idleVillagers, ControlGroupService groups, UnitService units, BuildingService buildings,
            NormalModeSettings normalMode, GridMap gridMap, CameraController cameraController, IEventBus eventBus, GameInputActions inputActions)
        {
            _selection = selection;
            _commands = commands;
            _targeting = targeting;
            _idleVillagers = idleVillagers;
            _groups = groups;
            _units = units;
            _buildings = buildings;
            _normalMode = normalMode;
            _gridMap = gridMap;
            _camera = cameraController;
            _eventBus = eventBus;
            _actions = inputActions.Gameplay;
        }

        /// <summary>True if the selection holds a unit of the player, so the command buttons and keys have something to order.</summary>
        public bool HasOwnUnitSelected
        {
            get
            {
                foreach (var id in _selection.Selected)
                {
                    if (_units.TryGet(id, out var unit) && unit.Faction == Faction.Player)
                        return true;
                }

                return false;
            }
        }

        public void PostTick()
        {
            // Placing turns the map off; a targeting left from before must not wait behind it.
            if (!_actions.enabled || (_targeting.IsActive && !HasOwnUnitSelected))
                _targeting.End();

            if (!_actions.enabled)
                return;

            if (_actions.IdleVillager.WasPressedThisFrame())
                SelectNextIdleVillager();

            if (_actions.ControlGroup.WasPressedThisFrame())
                OnGroupKey();

            if (_actions.AttackMove.WasPressedThisFrame())
                BeginAttackMove();

            if (_actions.Stop.WasPressedThisFrame())
                Stop();

            if (_actions.HoldPosition.WasPressedThisFrame())
                HoldPosition();

            if (_actions.FocusHome.WasPressedThisFrame())
                LookAtCastle();
        }

        /// <returns>False when no villager is idle.</returns>
        public bool SelectNextIdleVillager()
        {
            var villager = _idleVillagers.FindNext();
            if (villager == null)
                return false;

            _selection.Select(villager.Id);
            _camera.LookAt(villager.Position);
            return true;
        }

        public void BeginAttackMove()
        {
            if (HasOwnUnitSelected)
                _targeting.Begin();
        }

        public void Stop()
        {
            _targeting.End();
            _commands.Stop(_selection.Selected);
        }

        public void HoldPosition()
        {
            _targeting.End();
            _commands.HoldPosition(_selection.Selected);
        }

        private void OnGroupKey()
        {
            // The digit keys are bound in order, 1 to 9.
            if (!(_actions.ControlGroup.activeControl is KeyControl key))
                return;

            var group = key.keyCode - UnityEngine.InputSystem.Key.Digit1 + 1;
            if (group < 1 || group > ControlGroupService.GroupCount)
                return;

            if (_actions.GroupModifier.IsPressed())
                AssignGroup(group);
            else
                SelectGroup(group);
        }

        private void AssignGroup(int group)
        {
            var count = _groups.Assign(group, _selection.Selected);
            if (count > 0 && _groups.TryGetCenter(group, out var center))
                _eventBus.Publish(new FloatingTextRequestedEvent($"Group {group}", center));
        }

        private void SelectGroup(int group)
        {
            var members = _groups.GetLiving(group);
            if (members.Count == 0)
                return;

            _selection.Select(members);

            var isDoubleTap = group == _lastGroup && Time.unscaledTime - _lastGroupTime <= DoubleTapSeconds;
            if (isDoubleTap && _groups.TryGetCenter(group, out var center))
                _camera.LookAt(center);

            _lastGroup = group;
            _lastGroupTime = Time.unscaledTime;
        }

        private void LookAtCastle()
        {
            foreach (var building in _buildings.Buildings)
            {
                if (building.Definition != _normalMode.Castle || building.Faction != Faction.Player)
                    continue;

                _camera.LookAt(_gridMap.Origin + building.Area.center);
                return;
            }
        }
    }
}
