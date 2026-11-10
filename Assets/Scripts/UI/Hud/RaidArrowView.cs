using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Gameplay.CameraControl;
using BarracksAndBolts.Gameplay.Economy;
using BarracksAndBolts.Gameplay.Raids;
using BarracksAndBolts.Utils.Disposables;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace BarracksAndBolts.UI.Hud
{
    /// <summary>
    /// While a raid wave is on the map and out of view, an arrow at the screen edge points at the nearest raider.
    /// Clicking it moves the camera there.
    /// The root must stretch over the whole screen; the arrow is a child of it.
    /// </summary>
    public sealed class RaidArrowView : MonoBehaviour
    {
        [SerializeField] private RectTransform arrow;
        [Tooltip("Turns to point at the raid. The sprite must point left.")]
        [SerializeField] private RectTransform arrowImage;
        [SerializeField] private Button button;
        [Tooltip("Distance from the screen edge, in canvas units.")]
        [SerializeField, Min(0f)] private float edgeMargin = 70f;
        [SerializeField, Min(0f)] private float pulseSize = 0.08f;
        [SerializeField, Min(0f)] private float pulseSpeed = 6f;

        private readonly CompositeDisposable _subscriptions = new();
        private RaidService _raids;
        private CameraController _camera;
        private IEventBus _eventBus;
        private RectTransform _root;
        private Canvas _canvas;
        private Vector2 _raiderPosition;
        private bool _isGameOver;

        [Inject]
        private void Construct(RaidService raids, CameraController cameraController, IEventBus eventBus)
        {
            _raids = raids;
            _camera = cameraController;
            _eventBus = eventBus;
        }

        private void Awake()
        {
            _root = (RectTransform)transform;
            _canvas = GetComponentInParent<Canvas>().rootCanvas;
            arrow.gameObject.SetActive(false);
        }

        private void Start()
        {
            button.onClick.AddListener(OnClicked);
            _eventBus.Subscribe<GameEndedEvent>(_ => _isGameOver = true).AddTo(_subscriptions);
        }

        private void OnDestroy()
        {
            button.onClick.RemoveListener(OnClicked);
            _subscriptions.Dispose();
        }

        // LateUpdate, so the camera has already moved this frame.
        private void LateUpdate()
        {
            var screenSize = new Vector2(Screen.width, Screen.height);
            var isShown = !_isGameOver && _raids.TryGetNearestRaider(_camera.Center, out _raiderPosition);
            var target = isShown ? _camera.WorldToScreen(_raiderPosition) : Vector2.zero;
            isShown = isShown && !OffScreenArrowMath.IsOnScreen(target, screenSize);

            if (arrow.gameObject.activeSelf != isShown)
                arrow.gameObject.SetActive(isShown);

            if (!isShown)
                return;

            var edge = OffScreenArrowMath.GetEdgePoint(target, screenSize, edgeMargin * _canvas.scaleFactor);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, edge, null, out var local);
            arrow.localPosition = local;

            // The sprite points left, so 180 degrees turns it to the right (angle 0).
            arrowImage.localRotation = Quaternion.Euler(0f, 0f, OffScreenArrowMath.GetAngle(target, screenSize) - 180f);

            // On the image, not the button: ButtonFeedback scales the button on hover.
            arrowImage.localScale = Vector3.one * (1f + Mathf.Sin(Time.unscaledTime * pulseSpeed) * pulseSize);
        }

        private void OnClicked()
        {
            _camera.LookAt(_raiderPosition);
        }
    }
}
