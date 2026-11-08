using UnityEngine;

namespace BarracksAndBolts.UI.Common
{
    public static class PanelSizeMath
    {
        /// <summary>The height of a panel that hugs its content, within its limits.</summary>
        /// <param name="contentHeight">Height the content wants.</param>
        /// <param name="chromeHeight">Space the panel adds around the content: margins and frame.</param>
        /// <param name="availableHeight">Room on screen. Beats every other limit, so the panel never leaves the screen.</param>
        public static float FitHeight(float contentHeight, float chromeHeight, float minHeight, float maxHeight, float availableHeight)
        {
            var height = Mathf.Clamp(contentHeight + chromeHeight, minHeight, Mathf.Max(minHeight, maxHeight));
            return Mathf.Min(height, availableHeight);
        }
    }
}
