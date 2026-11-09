using BarracksAndBolts.Gameplay.Grid;
using UnityEngine;

namespace BarracksAndBolts.Gameplay.Territory
{
    /// <summary>
    /// Paints the territory on the ground while a building is being placed: a red tint outside, a line along the border
    /// and a green tint on what the building would open. One texture over the whole map, so it costs a single draw
    /// and is redrawn only when the territory or the previewed circle changes.
    /// </summary>
    public sealed class TerritoryOverlayView : MonoBehaviour
    {
        // Enough for a clean line along the cell edges, and little enough for a quick redraw.
        private const int PixelsPerCell = 8;

        [SerializeField] private SpriteRenderer spriteRenderer;
        [Tooltip("Strong enough to still read as red over the green grass.")]
        [SerializeField] private Color outsideColor = new(1f, 0.1f, 0.1f, 0.5f);
        [SerializeField] private Color borderColor = new(1f, 0.95f, 0.65f, 0.9f);
        [Tooltip("Cells the building being placed would open.")]
        [SerializeField] private Color previewColor = new(0.45f, 1f, 0.45f, 0.35f);

        private TerritoryService _territory;
        private GridMap _gridMap;
        private Texture2D _texture;
        private Sprite _sprite;
        private Color32[] _pixels;
        private int _drawnVersion = -1;
        private Vector2 _previewCenter;
        private float _previewRadius;
        private bool _isPreviewDirty;

        public void Bind(TerritoryService territory, GridMap gridMap)
        {
            _territory = territory;
            _gridMap = gridMap;

            var width = gridMap.Width * PixelsPerCell;
            var height = gridMap.Height * PixelsPerCell;
            _pixels = new Color32[width * height];
            _texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            _sprite = Sprite.Create(_texture, new Rect(0f, 0f, width, height), Vector2.zero, PixelsPerCell, 0, SpriteMeshType.FullRect);

            spriteRenderer.sprite = _sprite;
            spriteRenderer.enabled = false;
            transform.position = gridMap.Origin;
        }

        /// <summary>Shows what a building would open. Center is in cell coordinates; a radius of 0 shows nothing.</summary>
        public void SetPreview(Vector2 center, float radius)
        {
            if (center == _previewCenter && Mathf.Approximately(radius, _previewRadius))
                return;

            _previewCenter = center;
            _previewRadius = radius;
            _isPreviewDirty = true;
        }

        public void SetVisible(bool isVisible)
        {
            spriteRenderer.enabled = isVisible;
            if (isVisible && (_isPreviewDirty || _drawnVersion != _territory.Version))
                Redraw();
        }

        private void OnDestroy()
        {
            Destroy(_sprite);
            Destroy(_texture);
        }

        private void Redraw()
        {
            _drawnVersion = _territory.Version;
            _isPreviewDirty = false;
            for (var y = 0; y < _gridMap.Height; y++)
            {
                for (var x = 0; x < _gridMap.Width; x++)
                    DrawCell(new Vector2Int(x, y));
            }

            _texture.SetPixels32(_pixels);
            _texture.Apply(false);
        }

        private void DrawCell(Vector2Int cell)
        {
            var isOwned = _territory.IsInside(cell);
            var isOpen = isOwned || IsPreviewed(cell);
            var fill = isOwned ? Color.clear : isOpen ? previewColor : outsideColor;

            var hasLeftEdge = isOpen && !IsOpen(cell + Vector2Int.left);
            var hasRightEdge = isOpen && !IsOpen(cell + Vector2Int.right);
            var hasBottomEdge = isOpen && !IsOpen(cell + Vector2Int.down);
            var hasTopEdge = isOpen && !IsOpen(cell + Vector2Int.up);

            for (var py = 0; py < PixelsPerCell; py++)
            {
                for (var px = 0; px < PixelsPerCell; px++)
                {
                    var isOnEdge = (hasLeftEdge && px == 0) || (hasRightEdge && px == PixelsPerCell - 1) ||
                                   (hasBottomEdge && py == 0) || (hasTopEdge && py == PixelsPerCell - 1);
                    var index = (cell.y * PixelsPerCell + py) * _gridMap.Width * PixelsPerCell + cell.x * PixelsPerCell + px;
                    _pixels[index] = isOnEdge ? borderColor : fill;
                }
            }
        }

        private bool IsOpen(Vector2Int cell)
        {
            return _territory.IsInside(cell) || IsPreviewed(cell);
        }

        private bool IsPreviewed(Vector2Int cell)
        {
            return _gridMap.IsInBounds(cell) && TerritoryService.IsInCircle(cell, _previewCenter, _previewRadius);
        }
    }
}
