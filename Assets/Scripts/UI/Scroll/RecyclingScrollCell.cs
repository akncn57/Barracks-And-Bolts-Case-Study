using UnityEngine;

namespace BarracksAndBolts.UI.Scroll
{
    /// <summary>Base of every cell a <see cref="RecyclingScrollView"/> can show.</summary>
    [RequireComponent(typeof(RectTransform))]
    public abstract class RecyclingScrollCell : MonoBehaviour
    {
        private RectTransform _rectTransform;

        public RectTransform RectTransform
        {
            get
            {
                if (_rectTransform == null)
                    _rectTransform = (RectTransform)transform;

                return _rectTransform;
            }
        }
    }
}
