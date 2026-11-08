using System.Collections.Generic;
using UnityEngine;

namespace BarracksAndBolts.Gameplay.ResourceNodes
{
    /// <summary>
    /// Shows one resource. The sprite's pivot is on its base and the renderer sorts by the pivot,
    /// so units walking behind a tree are drawn behind it.
    /// </summary>
    public sealed class ResourceNodeView : MonoBehaviour
    {
        // Spreads the nodes over the animation by id, so a forest doesn't sway in perfect sync. A fraction of a loop per id.
        private const float PhasePerId = 0.37f;

        // Nodes of a row have the same height, and Unity may draw equal ones in a new order whenever a sprite changes.
        // Overlapping trees would swap places every frame, so each column sits a bit higher. Far less than a pixel.
        private const float SortNudgePerColumn = 0.0001f;

        [SerializeField] private SpriteRenderer spriteRenderer;

        private IReadOnlyList<Sprite> _frames;
        private float _phase;
        private int _shownFrame;

        public bool IsAnimated => _frames.Count > 1;

        public void Bind(ResourceNodeModel node, Vector2 position)
        {
            _frames = node.Definition.Frames;
            _phase = node.Id.Value * PhasePerId;
            name = $"{node.Definition.DisplayName} ({node.Id})";
            transform.position = position + Vector2.up * (node.Area.x * SortNudgePerColumn);
            spriteRenderer.sprite = _frames[0];
        }

        public void ShowFrameAt(float frameTime)
        {
            var frame = Mathf.FloorToInt(frameTime + _phase * _frames.Count) % _frames.Count;
            if (frame == _shownFrame)
                return;

            _shownFrame = frame;
            spriteRenderer.sprite = _frames[frame];
        }
    }
}
