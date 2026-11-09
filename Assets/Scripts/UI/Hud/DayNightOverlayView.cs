using BarracksAndBolts.Gameplay.Calendar;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace BarracksAndBolts.UI.Hud
{
    /// <summary>A dark tint over the map at night, drawn behind the panels. It doesn't catch the mouse.</summary>
    public sealed class DayNightOverlayView : MonoBehaviour
    {
        [SerializeField] private Image tint;
        [SerializeField, Range(0f, 1f)] private float nightAlpha = 0.4f;

        private CalendarService _calendar;

        [Inject]
        private void Construct(CalendarService calendar)
        {
            _calendar = calendar;
        }

        private void LateUpdate()
        {
            var alpha = _calendar.Darkness * nightAlpha;
            var isVisible = alpha > 0f;
            if (tint.enabled != isVisible)
                tint.enabled = isVisible;

            if (!isVisible)
                return;

            var color = tint.color;
            color.a = alpha;
            tint.color = color;
        }
    }
}
