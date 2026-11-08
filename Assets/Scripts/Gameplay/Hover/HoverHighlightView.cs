using BarracksAndBolts.Gameplay.Teams;
using UnityEngine;
using VContainer;

namespace BarracksAndBolts.Gameplay.Hover
{
    /// <summary>Draws a frame around what the mouse is over. One frame is enough, since only one thing can be hovered.</summary>
    public sealed class HoverHighlightView : MonoBehaviour
    {
        [Tooltip("Sprite with a 9-slice border and Draw Mode set to Sliced, so the frame stays thin at any size.")]
        [SerializeField] private SpriteRenderer frameRenderer;
        [SerializeField] private Color playerColor = new(1f, 1f, 1f, 0.8f);
        [SerializeField] private Color enemyColor = new(1f, 0.4f, 0.35f, 0.8f);
        [Tooltip("Extra space around the entity, in world units.")]
        [SerializeField, Min(0f)] private float padding = 0.08f;

        private HoverService _hover;

        [Inject]
        private void Construct(HoverService hover)
        {
            _hover = hover;
        }

        private void Awake()
        {
            // Setting the size has no effect in Simple mode; the frame would stay at the sprite's small size.
            frameRenderer.drawMode = SpriteDrawMode.Sliced;
        }

        private void LateUpdate()
        {
            var hasTarget = _hover.TryGetInfo(out var info);
            frameRenderer.enabled = hasTarget;
            if (!hasTarget)
                return;

            frameRenderer.transform.position = info.Bounds.center;
            frameRenderer.size = info.Bounds.size + Vector2.one * (padding * 2f);
            frameRenderer.color = info.Faction == Faction.Player ? playerColor : enemyColor;
        }
    }
}
