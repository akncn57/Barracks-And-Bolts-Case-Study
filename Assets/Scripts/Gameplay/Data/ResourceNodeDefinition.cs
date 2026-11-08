using System;
using System.Collections.Generic;
using UnityEngine;

namespace BarracksAndBolts.Gameplay.Data
{
    public enum ResourceKind
    {
        Wood,
        Stone,
        Food,
        Gold
    }

    /// <summary>Design data of one thing on the map that gives a resource: a tree, a rock, a gold mine or a sheep.</summary>
    [CreateAssetMenu(fileName = "Data_Resource_New", menuName = "Barracks & Bolts/Data/Resource Node")]
    public sealed class ResourceNodeDefinition : ScriptableObject
    {
        [Tooltip("Stable key for save files. Don't change it once saves exist.")]
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private ResourceKind kind;
        [Tooltip("Footprint in cells. The sprite can be taller.")]
        [SerializeField] private Vector2Int size = Vector2Int.one;
        [Tooltip("One sprite for a still node, more for a looping animation. Pivots must be on the node's base.")]
        [SerializeField] private Sprite[] frames = Array.Empty<Sprite>();

        public string Id => id;
        public string DisplayName => displayName;
        public ResourceKind Kind => kind;
        public Vector2Int Size => size;
        public IReadOnlyList<Sprite> Frames => frames;

        private void OnValidate()
        {
            size = Vector2Int.Max(size, Vector2Int.one);

            if (string.IsNullOrWhiteSpace(id))
                Debug.LogWarning($"{name}: id is empty.", this);

            if (frames.Length == 0)
                Debug.LogWarning($"{name}: there are no frames.", this);
        }
    }
}
