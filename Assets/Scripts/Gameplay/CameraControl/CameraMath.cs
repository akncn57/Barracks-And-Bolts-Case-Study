using UnityEngine;

namespace BarracksAndBolts.Gameplay.CameraControl
{
    /// <summary>
    /// Pure camera calculations, kept apart from the MonoBehaviour so they can be unit tested.
    /// </summary>
    public static class CameraMath
    {
        public static Vector2 GetViewHalfExtents(float orthographicSize, float aspect)
        {
            return new Vector2(orthographicSize * aspect, orthographicSize);
        }

        /// <summary>World units covered by one screen pixel.</summary>
        public static float GetWorldUnitsPerPixel(float orthographicSize, float screenHeight)
        {
            return screenHeight > 0f ? orthographicSize * 2f / screenHeight : 0f;
        }

        /// <summary>
        /// Screen pixel to world point for an orthographic view. Computed from our own center and size instead of
        /// Camera.ScreenToWorldPoint, because Cinemachine moves the real camera later in the frame.
        /// </summary>
        public static Vector2 ScreenToWorld(Vector2 screenPoint, Vector2 screenSize, Vector2 viewCenter, float orthographicSize)
        {
            if (screenSize.x <= 0f || screenSize.y <= 0f)
                return viewCenter;

            var halfExtents = GetViewHalfExtents(orthographicSize, screenSize.x / screenSize.y);
            var fromCenter = screenPoint / screenSize * 2f - Vector2.one;
            return viewCenter + Vector2.Scale(fromCenter, halfExtents);
        }

        /// <summary>Keeps the view inside the bounds. On an axis where the view is larger than the bounds, centers it.</summary>
        public static Vector2 ClampCenter(Vector2 center, Vector2 viewHalfExtents, Rect bounds)
        {
            return new Vector2(
                ClampAxis(center.x, viewHalfExtents.x, bounds.xMin, bounds.xMax),
                ClampAxis(center.y, viewHalfExtents.y, bounds.yMin, bounds.yMax));
        }

        /// <summary>Moves the center so the world point under the cursor stays under the cursor after zooming.</summary>
        public static Vector2 ZoomTowards(Vector2 center, Vector2 focus, float oldSize, float newSize)
        {
            if (oldSize <= 0f)
                return center;

            return focus + (center - focus) * (newSize / oldSize);
        }

        /// <summary>
        /// Direction to pan when the pointer touches a screen edge. Zero when the pointer is outside the window,
        /// so moving the mouse to another monitor doesn't scroll the map.
        /// </summary>
        public static Vector2 GetEdgePanDirection(Vector2 pointer, Vector2 screenSize, float edgeThickness)
        {
            if (pointer.x < 0f || pointer.y < 0f || pointer.x > screenSize.x || pointer.y > screenSize.y)
                return Vector2.zero;

            var direction = Vector2.zero;
            if (pointer.x <= edgeThickness)
                direction.x = -1f;
            else if (pointer.x >= screenSize.x - edgeThickness)
                direction.x = 1f;

            if (pointer.y <= edgeThickness)
                direction.y = -1f;
            else if (pointer.y >= screenSize.y - edgeThickness)
                direction.y = 1f;

            // Normalized so diagonal edge panning isn't faster than straight panning.
            return direction.normalized;
        }

        private static float ClampAxis(float center, float halfExtent, float min, float max)
        {
            // A clamp range would be inverted here, which makes the camera jump between edges.
            if (halfExtent * 2f >= max - min)
                return (min + max) * 0.5f;

            return Mathf.Clamp(center, min + halfExtent, max - halfExtent);
        }
    }
}
