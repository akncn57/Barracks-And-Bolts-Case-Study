using BarracksAndBolts.Gameplay.Data;
using BarracksAndBolts.Gameplay.Entities;
using UnityEngine;

namespace BarracksAndBolts.Gameplay.ResourceNodes
{
    /// <summary>Runtime state of one resource on the map.</summary>
    public sealed class ResourceNodeModel
    {
        public ResourceNodeModel(GameEntityId id, ResourceNodeDefinition definition, RectInt area)
        {
            Id = id;
            Definition = definition;
            Area = area;
        }

        public GameEntityId Id { get; }
        public ResourceNodeDefinition Definition { get; }

        /// <summary>Cells covered by the footprint.</summary>
        public RectInt Area { get; }
    }
}
