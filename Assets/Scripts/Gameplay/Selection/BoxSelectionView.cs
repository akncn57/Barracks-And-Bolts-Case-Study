using UnityEngine;
using VContainer;

namespace BarracksAndBolts.Gameplay.Selection
{
    /// <summary>Draws the selection box while it is being dragged. Only reads <see cref="SelectionService"/>.</summary>
    public sealed class BoxSelectionView : MonoBehaviour
    {
        [Tooltip("Sprite with a 9-slice border and Draw Mode set to Sliced, so the frame stays thin at any size.")]
        [SerializeField] private SpriteRenderer boxRenderer;

        private SelectionService _selection;

        [Inject]
        private void Construct(SelectionService selection)
        {
            _selection = selection;
        }

        private void LateUpdate()
        {
            boxRenderer.enabled = _selection.HasBox;
            if (!_selection.HasBox)
                return;

            var box = _selection.Box;
            boxRenderer.transform.position = box.center;
            boxRenderer.size = box.size;
        }
    }
}
