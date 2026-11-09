using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Gameplay.CameraControl;
using BarracksAndBolts.Gameplay.Hover;
using BarracksAndBolts.Gameplay.Teams;
using BarracksAndBolts.Utils.Disposables;
using TMPro;
using UnityEngine;
using VContainer;

namespace BarracksAndBolts.UI.Feedback
{
    /// <summary>Shows the name of what the mouse is over, right above it. The pivot of this object must be on its bottom middle.</summary>
    public sealed class HoverLabelView : MonoBehaviour
    {
        [Tooltip("Child that holds the visible label. Hidden while nothing is under the mouse; this object stays active.")]
        [SerializeField] private GameObject content;
        [SerializeField] private TMP_Text label;
        [SerializeField] private Color playerColor = Color.white;
        [SerializeField] private Color enemyColor = new(1f, 0.55f, 0.5f);

        [Header("Placement")]
        [Tooltip("Gap between the thing and the label, in world units.")]
        [SerializeField, Min(0f)] private float worldGap = 0.1f;
        [Tooltip("Kept free at the top of the screen for the resource bar. In canvas units.")]
        [SerializeField, Min(0f)] private float topMargin = 90f;

        private readonly CompositeDisposable _subscriptions = new();
        private HoverService _hover;
        private CameraController _camera;
        private IEventBus _eventBus;
        private RectTransform _rect;
        private RectTransform _parent;
        private Camera _canvasCamera;

        [Inject]
        private void Construct(HoverService hover, CameraController cameraController, IEventBus eventBus)
        {
            _hover = hover;
            _camera = cameraController;
            _eventBus = eventBus;
        }

        private void Start()
        {
            _rect = (RectTransform)transform;
            _parent = (RectTransform)transform.parent;

            var canvas = GetComponentInParent<Canvas>().rootCanvas;
            _canvasCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

            _eventBus.Subscribe<HoverChangedEvent>(_ => Refresh()).AddTo(_subscriptions);
            Refresh();
        }

        private void OnDestroy()
        {
            _subscriptions.Dispose();
        }

        // Every frame, because the camera and the unit under the mouse keep moving.
        private void LateUpdate()
        {
            if (!_hover.TryGetInfo(out var info))
            {
                if (content.activeSelf)
                    content.SetActive(false);

                return;
            }

            var bounds = info.Bounds;
            var screenPoint = _camera.WorldToScreen(new Vector2(bounds.center.x, bounds.yMax + worldGap));
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_parent, screenPoint, _canvasCamera, out var point))
                return;

            var area = _parent.rect;
            var size = _rect.rect.size;
            point.x = Mathf.Clamp(point.x, area.xMin + size.x * 0.5f, area.xMax - size.x * 0.5f);
            point.y = Mathf.Clamp(point.y, area.yMin, area.yMax - topMargin - size.y);
            _rect.localPosition = point;
        }

        private void Refresh()
        {
            var hasTarget = _hover.TryGetInfo(out var info);
            content.SetActive(hasTarget);
            if (!hasTarget)
                return;

            var isPlayer = info.Faction == Faction.Player;
            label.text = isPlayer ? info.Name : $"Enemy {info.Name}";
            label.color = isPlayer ? playerColor : enemyColor;
        }
    }
}
