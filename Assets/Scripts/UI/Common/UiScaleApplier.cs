using BarracksAndBolts.Core.Settings;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace BarracksAndBolts.UI.Common
{
    /// <summary>
    /// Applies the UI Size setting. The canvas scales to the screen from a reference resolution;
    /// a smaller reference makes everything bigger, so the size setting divides it.
    /// </summary>
    [RequireComponent(typeof(CanvasScaler))]
    public sealed class UiScaleApplier : MonoBehaviour
    {
        [SerializeField] private Vector2 baseResolution = new(1920f, 1080f);

        private CanvasScaler _scaler;
        private GameSettings _settings;

        [Inject]
        private void Construct(GameSettings settings)
        {
            _settings = settings;
            _scaler = GetComponent<CanvasScaler>();
            _settings.Changed += Apply;
            Apply();
        }

        private void OnDestroy()
        {
            if (_settings != null)
                _settings.Changed -= Apply;
        }

        private void Apply()
        {
            _scaler.referenceResolution = baseResolution / _settings.UiScale;
        }
    }
}
