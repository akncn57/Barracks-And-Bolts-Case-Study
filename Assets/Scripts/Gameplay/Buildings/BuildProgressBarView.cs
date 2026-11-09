using UnityEngine;

namespace BarracksAndBolts.Gameplay.Buildings
{
    /// <summary>
    /// A small bar over a building site that fills as the villagers work. Hidden on finished buildings.
    /// The frame is 9-sliced, so it stretches with the building's width; the fill is a one pixel wide column scaled sideways.
    /// </summary>
    public sealed class BuildProgressBarView : MonoBehaviour
    {
        // The frame texture is 64 pixels per unit. Its inner track starts 10 pixels in from each end
        // and 5 pixels up from the bottom edge.
        private const float PixelsPerUnit = 64f;
        private const float TrackInsetPixels = 10f;
        private const float TrackBottomPixels = 5f;

        [Tooltip("Sliced frame. Its pivot is in the middle.")]
        [SerializeField] private SpriteRenderer frameRenderer;
        [Tooltip("One pixel wide column with its pivot on the bottom-left, placed at the start of the track.")]
        [SerializeField] private SpriteRenderer fillRenderer;
        [Tooltip("Bar width per footprint cell, in world units.")]
        [SerializeField, Min(0.1f)] private float widthPerCell = 0.75f;
        [SerializeField, Min(0.5f)] private float minWidth = 1f;
        [SerializeField, Min(0.5f)] private float maxWidth = 2f;
        [Tooltip("Space between the top of the building and the bar, in world units.")]
        [SerializeField] private float gapAboveRoof = 0.3f;

        private float _width;
        private float _shownProgress = -1f;

        /// <param name="footprintWidth">In cells.</param>
        /// <param name="roofHeight">How tall the building looks, in world units from its base.</param>
        public void Show(int footprintWidth, float roofHeight)
        {
            // Whole pixels keep the frame crisp.
            _width = Mathf.Round(Mathf.Clamp(footprintWidth * widthPerCell, minWidth, maxWidth) * PixelsPerUnit) / PixelsPerUnit;
            transform.localPosition = new Vector3(0f, roofHeight + gapAboveRoof, 0f);
            frameRenderer.enabled = true;
            frameRenderer.drawMode = SpriteDrawMode.Sliced;
            frameRenderer.size = new Vector2(_width, frameRenderer.sprite.bounds.size.y);

            var frameBottom = -frameRenderer.size.y * 0.5f;
            fillRenderer.transform.localPosition = new Vector3(-_width * 0.5f + TrackInsetPixels / PixelsPerUnit, frameBottom + TrackBottomPixels / PixelsPerUnit, 0f);

            _shownProgress = -1f;
            gameObject.SetActive(true);
            SetProgress(0f);
        }

        public void SetProgress(float progress)
        {
            progress = Mathf.Clamp01(progress);
            if (Mathf.Approximately(progress, _shownProgress))
                return;

            _shownProgress = progress;
            var fillWidth = GetFillWidth(_width, progress);

            // Scale 0 would still draw nothing, but turning it off says so plainly.
            fillRenderer.enabled = fillWidth > 0f;
            fillRenderer.transform.localScale = new Vector3(fillWidth * PixelsPerUnit, 1f, 1f);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        /// <summary>World width of the fill for a bar of this width, snapped to whole pixels so it grows in clean steps.</summary>
        public static float GetFillWidth(float barWidth, float progress)
        {
            var trackPixels = Mathf.Max(barWidth * PixelsPerUnit - TrackInsetPixels * 2f, 0f);
            return Mathf.Floor(trackPixels * Mathf.Clamp01(progress)) / PixelsPerUnit;
        }
    }
}
