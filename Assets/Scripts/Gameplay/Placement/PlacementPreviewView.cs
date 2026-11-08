using System.Collections.Generic;
using BarracksAndBolts.Gameplay.Buildings;
using BarracksAndBolts.Gameplay.Data;
using BarracksAndBolts.Gameplay.Grid;
using BarracksAndBolts.Gameplay.Teams;
using UnityEngine;
using VContainer;

namespace BarracksAndBolts.Gameplay.Placement
{
    /// <summary>
    /// Ghost of the building being placed, with one tile per footprint cell: green where the cell is free, red where it isn't.
    /// Only reads <see cref="PlacementService"/>; input lives in <see cref="PlacementInputHandler"/>.
    /// </summary>
    public sealed class PlacementPreviewView : MonoBehaviour
    {
        [Tooltip("Parent of everything visible. Turned off while placement is inactive; this object keeps running.")]
        [SerializeField] private GameObject visuals;
        [SerializeField] private SpriteRenderer buildingRenderer;
        [Tooltip("Disabled child copied for each footprint cell. Its sprite is scaled to fill one cell.")]
        [SerializeField] private SpriteRenderer cellTemplate;

        [Header("Colors")]
        [SerializeField] private Color freeCellColor = new(0.3f, 1f, 0.3f, 0.45f);
        [SerializeField] private Color blockedCellColor = new(1f, 0.25f, 0.25f, 0.55f);
        [SerializeField] private Color validBuildingTint = new(1f, 1f, 1f, 0.75f);
        [SerializeField] private Color invalidBuildingTint = new(1f, 0.5f, 0.5f, 0.6f);
        [Tooltip("Share of a cell a tile covers. Below 1 leaves a gap, so single cells are easy to tell apart.")]
        [SerializeField, Range(0.5f, 1f)] private float cellFill = 0.92f;

        private readonly List<SpriteRenderer> _cells = new();
        private PlacementService _placement;
        private GridMap _gridMap;
        private TeamService _teams;
        private BuildingDefinition _shownDefinition;
        private Faction _shownFaction;

        [Inject]
        private void Construct(PlacementService placement, GridMap gridMap, TeamService teams)
        {
            _placement = placement;
            _gridMap = gridMap;
            _teams = teams;
        }

        private void Awake()
        {
            cellTemplate.gameObject.SetActive(false);
            FitCellTemplateToOneCell();
            visuals.SetActive(false);
        }

        // LateUpdate: placement input runs at the end of Update, so the footprint is already up to date here.
        private void LateUpdate()
        {
            var isVisible = _placement.IsActive && _placement.HasArea;
            if (visuals.activeSelf != isVisible)
                visuals.SetActive(isVisible);

            if (!isVisible)
                return;

            if (_shownDefinition != _placement.Definition || _shownFaction != _placement.Faction)
                ShowBuilding(_placement.Definition, _placement.Faction);

            var area = _placement.Area;
            transform.position = _gridMap.AreaToWorldBottomCenter(area);

            var isAreaFree = true;
            var cellIndex = 0;
            foreach (var cell in area.allPositionsWithin)
            {
                // IsWalkable is also false outside the map, so cells sticking out show red too.
                var isFree = _gridMap.IsWalkable(cell);
                isAreaFree &= isFree;

                var cellRenderer = _cells[cellIndex++];
                cellRenderer.transform.position = _gridMap.CellToWorldCenter(cell);
                cellRenderer.color = isFree ? freeCellColor : blockedCellColor;
            }

            buildingRenderer.color = isAreaFree ? validBuildingTint : invalidBuildingTint;
        }

        private void ShowBuilding(BuildingDefinition definition, Faction faction)
        {
            _shownDefinition = definition;
            _shownFaction = faction;

            var sprite = _teams.GetColorSet(faction).GetBuildingSprite(definition);
            BuildingSpriteLayout.PlaceOnFootprint(buildingRenderer, sprite);

            var cellCount = definition.Size.x * definition.Size.y;
            while (_cells.Count < cellCount)
                _cells.Add(Instantiate(cellTemplate, visuals.transform));

            // Keep the extra tiles of a bigger building for later instead of destroying them.
            for (var i = 0; i < _cells.Count; i++)
                _cells[i].gameObject.SetActive(i < cellCount);
        }

        private void FitCellTemplateToOneCell()
        {
            if (cellTemplate.sprite == null)
                return;

            // Scale by the sprite's own size, so the tile is right whatever its pixels-per-unit is.
            var spriteSize = cellTemplate.sprite.bounds.size;
            cellTemplate.transform.localScale = new Vector3(cellFill / spriteSize.x, cellFill / spriteSize.y, 1f);
        }
    }
}
