using UnityEngine;

namespace BarracksAndBolts.Gameplay.Buildings
{
    /// <summary>
    /// A circle on the map that shows how far a building reaches, like a tower's range.
    /// The texture is drawn when first shown and only again if the radius changes.
    /// </summary>
    public sealed class CoverageRingView : MonoBehaviour
    {
        // Same pixel size as the territory overlay, so both look like one style.
        private const int PixelsPerCell = 8;
        private const int LinePixels = 2;

        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Color lineColor = Color.white;
        [Tooltip("The inside is a faint version of the line, strong enough to see the area but not to hide the map.")]
        [SerializeField, Range(0f, 1f)] private float fillAlpha = 0.08f;

        private Texture2D _texture;
        private Sprite _sprite;
        private float _drawnRadius;

        /// <param name="localCenter">The middle of the circle, relative to the parent.</param>
        /// <param name="radius">In cells.</param>
        public void Show(Vector2 localCenter, float radius)
        {
            if (!Mathf.Approximately(radius, _drawnRadius))
                Draw(radius);

            transform.localPosition = localCenter;
            spriteRenderer.enabled = true;
        }

        public void Hide()
        {
            spriteRenderer.enabled = false;
        }

        /// <summary>Paints a circle that touches the edges of a square texture: a line along the rim, the fill inside and nothing outside.</summary>
        public static void PaintRing(Color32[] pixels, int size, int linePixels, Color32 line, Color32 fill)
        {
            var radius = size * 0.5f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(radius, radius));
                    pixels[y * size + x] = distance > radius ? default : distance > radius - linePixels ? line : fill;
                }
            }
        }

        private void OnDestroy()
        {
            ReleaseTexture();
        }

        private void Draw(float radius)
        {
            ReleaseTexture();
            _drawnRadius = radius;

            var size = Mathf.CeilToInt(radius * 2f * PixelsPerCell);
            var pixels = new Color32[size * size];
            var fill = lineColor;
            fill.a = fillAlpha;
            PaintRing(pixels, size, LinePixels, lineColor, fill);

            _texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            _texture.SetPixels32(pixels);
            _texture.Apply(false, true);

            _sprite = Sprite.Create(_texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), PixelsPerCell, 0, SpriteMeshType.FullRect);
            spriteRenderer.sprite = _sprite;
        }

        private void ReleaseTexture()
        {
            if (_sprite != null)
                Destroy(_sprite);

            if (_texture != null)
                Destroy(_texture);
        }
    }
}
