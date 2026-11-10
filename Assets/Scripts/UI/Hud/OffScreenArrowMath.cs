using UnityEngine;

namespace BarracksAndBolts.UI.Hud
{
    /// <summary>Where an arrow at the screen edge goes, pointing at a point outside the screen. All values in screen pixels.</summary>
    public static class OffScreenArrowMath
    {
        public static bool IsOnScreen(Vector2 point, Vector2 screenSize)
        {
            return point.x >= 0f && point.y >= 0f && point.x <= screenSize.x && point.y <= screenSize.y;
        }

        /// <summary>
        /// The point where the line from the screen center to the target meets the screen edge, moved in by the margin.
        /// </summary>
        public static Vector2 GetEdgePoint(Vector2 target, Vector2 screenSize, float margin)
        {
            var center = screenSize * 0.5f;
            var direction = target - center;
            var halfExtents = Vector2.Max(center - Vector2.one * margin, Vector2.zero);

            var scaleX = Mathf.Approximately(direction.x, 0f) ? float.MaxValue : halfExtents.x / Mathf.Abs(direction.x);
            var scaleY = Mathf.Approximately(direction.y, 0f) ? float.MaxValue : halfExtents.y / Mathf.Abs(direction.y);
            var scale = Mathf.Min(scaleX, scaleY);
            return scale == float.MaxValue ? center : center + direction * scale;
        }

        /// <summary>Angle from the screen center to the target, in degrees. 0 is right, 90 is up.</summary>
        public static float GetAngle(Vector2 target, Vector2 screenSize)
        {
            var direction = target - screenSize * 0.5f;
            return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        }
    }
}
