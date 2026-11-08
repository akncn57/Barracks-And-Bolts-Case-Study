using UnityEngine;

namespace BarracksAndBolts.UI.Tooltips
{
    public static class TooltipPlacement
    {
        /// <summary>
        /// Where a tooltip goes next to its anchor: on the side with more room, and never past the edges of the area.
        /// All rects are in the same space.
        /// </summary>
        /// <returns>The tooltip's bottom-left corner.</returns>
        public static Vector2 GetBottomLeft(Rect anchor, Vector2 size, Rect area, float gap)
        {
            var x = anchor.center.x < area.center.x ? anchor.xMax + gap : anchor.xMin - gap - size.x;
            var y = anchor.center.y - size.y * 0.5f;

            return new Vector2(KeepInside(x, area.xMin, area.xMax - size.x), KeepInside(y, area.yMin, area.yMax - size.y));
        }

        // Unlike Mathf.Clamp, this stays sensible when the tooltip is bigger than the area: the start edge wins.
        private static float KeepInside(float value, float min, float max)
        {
            return Mathf.Max(Mathf.Min(value, max), min);
        }
    }
}
