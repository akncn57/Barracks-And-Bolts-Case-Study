using UnityEngine;

namespace BarracksAndBolts.Gameplay.Buildings
{
    public static class BuildingSpriteLayout
    {
        /// <summary>
        /// Shows the sprite with its bottom-center on the parent's origin, whatever pivot the sprite was imported with.
        /// The parent sits on the bottom-center of the footprint, so the sprite stands on its cells.
        /// </summary>
        public static void PlaceOnFootprint(SpriteRenderer spriteRenderer, Sprite sprite)
        {
            spriteRenderer.sprite = sprite;
            if (sprite == null)
                return;

            // Sprite bounds are relative to the pivot, so this moves the bottom-center onto the parent.
            var bounds = sprite.bounds;
            spriteRenderer.transform.localPosition = new Vector3(-bounds.center.x, -bounds.min.y, 0f);
        }
    }
}
