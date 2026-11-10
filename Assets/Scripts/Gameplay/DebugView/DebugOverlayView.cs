using System.Collections.Generic;
using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Gameplay.Grid;
using BarracksAndBolts.Gameplay.Sandbox;
using BarracksAndBolts.Gameplay.Selection;
using BarracksAndBolts.Gameplay.Teams;
using BarracksAndBolts.Gameplay.Units;
using BarracksAndBolts.Utils.Disposables;
using UnityEngine;
using VContainer;

namespace BarracksAndBolts.Gameplay.DebugView
{
    /// <summary>
    /// Sandbox debug view: the grid that A* searches, and the path each selected unit is walking.
    /// Cells blocked by the ground (water, cliffs) are dark, cells taken by buildings or resources red, stairs blue.
    /// Off until the tester switches it on; while off it costs nothing.
    /// </summary>
    public sealed class DebugOverlayView : MonoBehaviour
    {
        private const int PixelsPerCell = 8;

        // Space between path dots, in world units.
        private const float DotSpacing = 0.3f;
        private const float GoalDotScale = 2f;

        // A long path of a big group could ask for thousands of dots; this is plenty to read it.
        private const int MaxDots = 800;

        [SerializeField] private SpriteRenderer gridRenderer;
        [Tooltip("Hidden template copied for each dot of a path.")]
        [SerializeField] private SpriteRenderer dotTemplate;

        [Header("Grid")]
        [SerializeField] private Color lineColor = new(0f, 0f, 0f, 0.18f);
        [SerializeField] private Color groundBlockedColor = new(0f, 0f, 0f, 0.35f);
        [SerializeField] private Color takenColor = new(1f, 0.2f, 0.2f, 0.4f);
        [SerializeField] private Color rampColor = new(0.3f, 0.6f, 1f, 0.4f);

        [Header("Paths")]
        [SerializeField] private Color playerPathColor = new(1f, 1f, 1f, 0.9f);
        [SerializeField] private Color enemyPathColor = new(1f, 0.45f, 0.4f, 0.9f);

        private readonly CompositeDisposable _subscriptions = new();
        private readonly List<SpriteRenderer> _dots = new();
        private readonly List<Vector2> _path = new();
        private GridMap _gridMap;
        private SandboxService _sandbox;
        private SelectionService _selection;
        private UnitService _units;
        private Texture2D _texture;
        private Sprite _sprite;
        private Color32[] _pixels;
        private bool _isGridDirty = true;

        [Inject]
        private void Construct(GridMap gridMap, SandboxService sandbox, SelectionService selection, UnitService units, IEventBus eventBus)
        {
            _gridMap = gridMap;
            _sandbox = sandbox;
            _selection = selection;
            _units = units;

            // Buildings and resources come and go; the texture is redrawn next time it is shown.
            eventBus.Subscribe<GridChangedEvent>(_ => _isGridDirty = true).AddTo(_subscriptions);

            dotTemplate.gameObject.SetActive(false);
            gridRenderer.enabled = false;
        }

        private void OnDestroy()
        {
            _subscriptions.Dispose();
            if (_sprite != null)
                Destroy(_sprite);

            if (_texture != null)
                Destroy(_texture);
        }

        private void LateUpdate()
        {
            if (_sandbox == null || !_sandbox.IsDebugViewOn)
            {
                if (gridRenderer.enabled)
                {
                    gridRenderer.enabled = false;
                    ShowDots(0);
                }

                return;
            }

            if (_isGridDirty)
                RedrawGrid();

            gridRenderer.enabled = true;
            DrawPaths();
        }

        private void RedrawGrid()
        {
            _isGridDirty = false;
            var width = _gridMap.Width * PixelsPerCell;
            var height = _gridMap.Height * PixelsPerCell;
            if (_texture == null)
            {
                _texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp
                };
                _sprite = Sprite.Create(_texture, new Rect(0f, 0f, width, height), Vector2.zero, PixelsPerCell, 0, SpriteMeshType.FullRect);
                _pixels = new Color32[width * height];
                gridRenderer.sprite = _sprite;
                gridRenderer.transform.position = _gridMap.Origin;
            }

            for (var cellY = 0; cellY < _gridMap.Height; cellY++)
            {
                for (var cellX = 0; cellX < _gridMap.Width; cellX++)
                {
                    var fill = GetCellColor(new Vector2Int(cellX, cellY));
                    for (var y = 0; y < PixelsPerCell; y++)
                    {
                        var row = (cellY * PixelsPerCell + y) * width + cellX * PixelsPerCell;
                        for (var x = 0; x < PixelsPerCell; x++)
                        {
                            // The cell's left and bottom edge, so each line between two cells is drawn once.
                            var isLine = x == 0 || y == 0;
                            _pixels[row + x] = isLine ? Blend(fill, lineColor) : fill;
                        }
                    }
                }
            }

            _texture.SetPixels32(_pixels);
            _texture.Apply(false);
        }

        private Color32 GetCellColor(Vector2Int cell)
        {
            if (!_gridMap.IsTerrainWalkable(cell))
                return groundBlockedColor;

            if (_gridMap.GetOccupant(cell).IsValid)
                return takenColor;

            return _gridMap.IsRamp(cell) ? rampColor : Color.clear;
        }

        private static Color32 Blend(Color under, Color over)
        {
            var alpha = over.a + under.a * (1f - over.a);
            if (alpha <= 0f)
                return Color.clear;

            var rgb = ((Color)over * over.a + under * under.a * (1f - over.a)) / alpha;
            return new Color(rgb.r, rgb.g, rgb.b, alpha);
        }

        private void DrawPaths()
        {
            var dotCount = 0;
            foreach (var id in _selection.Selected)
            {
                if (!_units.TryGet(id, out var unit) || !unit.IsMoving)
                    continue;

                unit.CopyRemainingPath(_path);
                var color = unit.Faction == Faction.Player ? playerPathColor : enemyPathColor;
                dotCount = DrawPath(unit.Position, color, dotCount);
            }

            ShowDots(dotCount);
        }

        /// <returns>How many dots are in use after this path.</returns>
        private int DrawPath(Vector2 from, Color color, int dotCount)
        {
            // Distance along the path to the next dot. The first one sits half a step ahead of the unit.
            var nextDotAt = DotSpacing * 0.5f;
            var previous = from;
            foreach (var point in _path)
            {
                var segment = point - previous;
                var length = segment.magnitude;
                while (nextDotAt <= length && dotCount < MaxDots)
                {
                    PlaceDot(dotCount++, previous + segment * (nextDotAt / length), color, 1f);
                    nextDotAt += DotSpacing;
                }

                nextDotAt -= length;
                previous = point;
            }

            if (_path.Count > 0 && dotCount < MaxDots)
                PlaceDot(dotCount++, _path[_path.Count - 1], color, GoalDotScale);

            return dotCount;
        }

        private void PlaceDot(int index, Vector2 position, Color color, float scale)
        {
            while (_dots.Count <= index)
                _dots.Add(Instantiate(dotTemplate, transform));

            var dot = _dots[index];
            dot.transform.position = position;
            dot.transform.localScale = Vector3.one * scale;
            dot.color = color;
        }

        // Dots are kept and reused; the extra ones are only switched off.
        private void ShowDots(int count)
        {
            for (var i = 0; i < _dots.Count; i++)
            {
                var isUsed = i < count;
                if (_dots[i].gameObject.activeSelf != isUsed)
                    _dots[i].gameObject.SetActive(isUsed);
            }
        }
    }
}
