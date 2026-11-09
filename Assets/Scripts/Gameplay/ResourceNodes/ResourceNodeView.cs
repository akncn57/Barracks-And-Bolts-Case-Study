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

        // A villager works with its feet on the node's base line. This keeps it drawn in front, so the swing shows.
        private const float BaseLift = 0.02f;

        [SerializeField] private SpriteRenderer spriteRenderer;

        private ResourceNodeModel _node;
        private IReadOnlyList<Sprite> _frames;
        private IReadOnlyList<Sprite> _shownFrames;
        private Vector2 _basePosition;
        private bool _roams;
        private float _phase;
        private int _shownFrame;

        public bool IsAnimated => _roams || _frames.Count > 1;

        public void Bind(ResourceNodeModel node, Vector2 position)
        {
            _node = node;
            _frames = node.Definition.Frames;
            _shownFrames = _frames;
            _roams = node.Definition.RoamRadius > 0f;
            _phase = node.Id.Value * PhasePerId;
            name = $"{node.Definition.DisplayName} ({node.Id})";
            _basePosition = position + Vector2.up * (BaseLift + node.Area.x * SortNudgePerColumn);
            transform.position = _basePosition;
            spriteRenderer.sprite = _frames[0];
        }

        public void ShowFrameAt(float frameTime)
        {
            if (_roams)
            {
                transform.position = _basePosition + _node.Offset;
                spriteRenderer.flipX = _node.IsFacingLeft;
            }

            var frames = _node.IsWalking && _node.Definition.WalkFrames.Count > 0 ? _node.Definition.WalkFrames : _frames;
            var frame = Mathf.FloorToInt(frameTime + _phase * frames.Count) % frames.Count;
            if (frame == _shownFrame && frames == _shownFrames)
                return;

            _shownFrames = frames;
            _shownFrame = frame;
            spriteRenderer.sprite = frames[frame];
        }
    }
}
