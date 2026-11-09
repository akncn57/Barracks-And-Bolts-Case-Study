using BarracksAndBolts.Gameplay.Data;
using BarracksAndBolts.Gameplay.Entities;
using UnityEngine;

namespace BarracksAndBolts.Gameplay.ResourceNodes
{
    /// <summary>Runtime state of one resource on the map.</summary>
    public sealed class ResourceNodeModel
    {
        // How long one Hold call lasts. A villager at work renews it every tick, so a cancelled order lets go by itself.
        private const float HoldTime = 0.5f;

        private Vector2 _walkGoal;
        private float _restLeft;
        private float _holdLeft;

        public ResourceNodeModel(GameEntityId id, ResourceNodeDefinition definition, RectInt area)
        {
            Id = id;
            Definition = definition;
            Area = area;
            Remaining = definition.Amount;
        }

        public GameEntityId Id { get; }
        public ResourceNodeDefinition Definition { get; }

        /// <summary>Cells covered by the footprint.</summary>
        public RectInt Area { get; }

        public int Remaining { get; private set; }
        public bool IsDepleted => Remaining <= 0;

        /// <summary>Where it stands, from the bottom middle of its footprint. Stays zero for things that don't roam.</summary>
        public Vector2 Offset { get; private set; }

        public bool IsWalking { get; private set; }
        public bool IsFacingLeft { get; private set; }

        /// <summary>Removes up to the given amount.</summary>
        /// <returns>How much there was to take.</returns>
        public int Take(int amount)
        {
            var taken = Mathf.Min(amount, Remaining);
            Remaining -= taken;
            return taken;
        }

        /// <summary>Keeps it still for a moment, so a villager can get to it.</summary>
        public void Hold()
        {
            _holdLeft = HoldTime;
            IsWalking = false;
        }

        /// <summary>Starts walking to the offset, then rests there for the given seconds.</summary>
        public void WalkTo(Vector2 goal, float restAfter)
        {
            _walkGoal = goal;
            _restLeft = restAfter;
            IsWalking = true;
            IsFacingLeft = goal.x < Offset.x;
        }

        /// <returns>True when it has rested enough and wants a new place to go.</returns>
        public bool TickRoam(float deltaTime, float walkSpeed)
        {
            if (_holdLeft > 0f)
            {
                _holdLeft -= deltaTime;
                return false;
            }

            if (!IsWalking)
            {
                _restLeft -= deltaTime;
                return _restLeft <= 0f;
            }

            Offset = Vector2.MoveTowards(Offset, _walkGoal, walkSpeed * deltaTime);
            if (Offset == _walkGoal)
                IsWalking = false;

            return false;
        }
    }
}
