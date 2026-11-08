using BarracksAndBolts.Gameplay.Entities;
using UnityEngine;

namespace BarracksAndBolts.Gameplay.Buildings
{
    /// <summary>
    /// Shows one building. The root sits on the bottom-center of the footprint; a Sorting Group on the root
    /// makes the building sort by its base instead of the sprite center, so units behind it are drawn behind it.
    /// </summary>
    public sealed class BuildingView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;

        public GameEntityId BuildingId { get; private set; }

        public void Bind(BuildingModel building, Sprite sprite, Vector2 position)
        {
            BuildingId = building.Id;
            name = $"{building.Definition.DisplayName} ({building.Id})";
            transform.position = position;
            BuildingSpriteLayout.PlaceOnFootprint(spriteRenderer, sprite);
        }
    }
}
