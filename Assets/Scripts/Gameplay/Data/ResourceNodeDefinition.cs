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
        [Tooltip("How tall it looks, in cells, counted from the bottom of the footprint. Clicks and hover use this. Never less than the footprint.")]
        [SerializeField, Min(0f)] private float visualHeight;
        [Tooltip("How much can be gathered before it is gone.")]
        [SerializeField, Min(1)] private int amount = 100;
        [Tooltip("How far from its middle a villager stands to work on it. The tool reaches about 0.9, so a bit less lands on it.")]
        [SerializeField, Min(0f)] private float workDistance = 0.6f;
        [Tooltip("One sprite for a still node, more for a looping animation. Pivots must be on the node's base.")]
        [SerializeField] private Sprite[] frames = Array.Empty<Sprite>();

        [Header("Living things")]
        [Tooltip("How far it walks from its spot, in cells. Stay under 0.5 so it keeps to its own cell. Zero for things that stay put.")]
        [SerializeField, Range(0f, 0.45f)] private float roamRadius;
        [Tooltip("Frames while it walks. Pivots must be on its feet.")]
        [SerializeField] private Sprite[] walkFrames = Array.Empty<Sprite>();
        [Tooltip("One quick cut and it is gone: the villager carries all of it at once, whatever the carry capacity.")]
        [SerializeField] private bool isOneCut;

        public string Id => id;
        public string DisplayName => displayName;
        public ResourceKind Kind => kind;
        public Vector2Int Size => size;
        public float VisualHeight => Mathf.Max(visualHeight, size.y);
        public int Amount => amount;
        public float WorkDistance => workDistance;
        public IReadOnlyList<Sprite> Frames => frames;
        public float RoamRadius => roamRadius;
        public IReadOnlyList<Sprite> WalkFrames => walkFrames;
        public bool IsOneCut => isOneCut;

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
