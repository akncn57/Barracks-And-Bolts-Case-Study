using UnityEngine;

namespace BarracksAndBolts.Gameplay.Combat
{
    /// <summary>
    /// A thin health bar over a unit or building: green for the player, red for the enemy.
    /// The frame is 9-sliced to any width; the fill is a one pixel wide column scaled sideways in whole pixels.
    /// </summary>
    public sealed class HealthBarView : MonoBehaviour
    {
        // The frame texture is 64 pixels per unit. Its track starts one pixel in from every edge.
        private const float PixelsPerUnit = 64f;
        private const float TrackInsetPixels = 1f;

        [SerializeField] private SpriteRenderer frameRenderer;
        [Tooltip("One pixel wide column with its pivot on the bottom-left.")]
        [SerializeField] private SpriteRenderer fillRenderer;
        [SerializeField] private Color playerColor = new(0.35f, 0.85f, 0.35f, 1f);
        [SerializeField] private Color enemyColor = new(0.95f, 0.3f, 0.25f, 1f);

        private float _width;
        private float _shownRatio = -1f;

        /// <param name="width">Bar width in world units.</param>
        /// <param name="height">Middle of the bar, in world units above the owner's feet or base.</param>
        public void Layout(float width, float height, bool isPlayer)
        {
            // Whole pixels keep it crisp.
            _width = Mathf.Round(width * PixelsPerUnit) / PixelsPerUnit;
            transform.localPosition = new Vector3(0f, height, 0f);
            frameRenderer.drawMode = SpriteDrawMode.Sliced;
            frameRenderer.size = new Vector2(_width, frameRenderer.sprite.bounds.size.y);

            var inset = TrackInsetPixels / PixelsPerUnit;
            fillRenderer.transform.localPosition = new Vector3(-_width * 0.5f + inset, -frameRenderer.size.y * 0.5f + inset, 0f);
            fillRenderer.color = isPlayer ? playerColor : enemyColor;
            Hide();
        }

        /// <param name="ratio">Health left, 0 to 1.</param>
        public void Show(float ratio)
        {
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            ratio = Mathf.Clamp01(ratio);
            if (Mathf.Approximately(ratio, _shownRatio))
                return;

            _shownRatio = ratio;
            var fillWidth = GetFillWidth(_width, ratio);
            fillRenderer.enabled = fillWidth > 0f;
            fillRenderer.transform.localScale = new Vector3(fillWidth * PixelsPerUnit, 1f, 1f);
        }

        public void Hide()
        {
            if (gameObject.activeSelf)
                gameObject.SetActive(false);
        }

        /// <summary>World width of the fill for a bar of this width, in whole pixels. Any health left shows at least one pixel.</summary>
        public static float GetFillWidth(float barWidth, float ratio)
        {
            var trackPixels = Mathf.Max(barWidth * PixelsPerUnit - TrackInsetPixels * 2f, 0f);
            var pixels = Mathf.Floor(trackPixels * Mathf.Clamp01(ratio));
            if (ratio > 0f && pixels < 1f && trackPixels >= 1f)
                pixels = 1f;

            return pixels / PixelsPerUnit;
        }
    }
}
