using System;
using UnityEngine;

namespace BarracksAndBolts.Gameplay.Data
{
    /// <summary>An amount of one resource: part of a price, or part of what a game starts with.</summary>
    [Serializable]
    public struct ResourceCost
    {
        [SerializeField] private ResourceKind kind;
        [SerializeField, Min(0)] private int amount;

        public ResourceCost(ResourceKind kind, int amount)
        {
            this.kind = kind;
            this.amount = amount;
        }

        public ResourceKind Kind => kind;
        public int Amount => amount;
    }
}
