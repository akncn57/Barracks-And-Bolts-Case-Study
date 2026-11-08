using UnityEngine;

namespace BarracksAndBolts.Gameplay.CameraControl
{
    [CreateAssetMenu(fileName = "Data_Config_Camera", menuName = "Barracks & Bolts/Configs/Camera")]
    public sealed class CameraConfig : ScriptableObject
    {
        [Header("Pan")]
        [Tooltip("Screen heights per second. Scaling with the view keeps panning equally fast at every zoom level.")]
        [SerializeField, Min(0f)] private float panSpeed = 1.2f;
        [SerializeField] private bool isEdgePanEnabled;
        [Tooltip("Distance from the screen edge, in pixels, that starts edge panning.")]
        [SerializeField, Min(1f)] private float edgeThickness = 12f;

        [Header("Zoom")]
        [Tooltip("Smallest orthographic size, i.e. the closest zoom.")]
        [SerializeField, Min(0.5f)] private float minZoom = 3f;
        [SerializeField, Min(0.5f)] private float maxZoom = 12f;
        [Tooltip("Orthographic size change per scroll notch.")]
        [SerializeField, Min(0.1f)] private float zoomStep = 1f;
        [Tooltip("Higher is snappier. Used for frame rate independent smoothing.")]
        [SerializeField, Min(0.1f)] private float zoomSharpness = 12f;

        [Header("Bounds")]
        [Tooltip("How far beyond the map edge the camera may show, in cells.")]
        [SerializeField, Min(0f)] private float boundsPadding = 1f;

        public float PanSpeed => panSpeed;
        public bool IsEdgePanEnabled => isEdgePanEnabled;
        public float EdgeThickness => edgeThickness;
        public float MinZoom => minZoom;
        public float MaxZoom => maxZoom;
        public float ZoomStep => zoomStep;
        public float ZoomSharpness => zoomSharpness;
        public float BoundsPadding => boundsPadding;

        private void OnValidate()
        {
            if (maxZoom < minZoom)
                maxZoom = minZoom;
        }
    }
}
