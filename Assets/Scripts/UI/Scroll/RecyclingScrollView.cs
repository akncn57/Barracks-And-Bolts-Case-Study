using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BarracksAndBolts.UI.Scroll
{
    /// <summary>
    /// Endless vertical grid. Creates only as many cells as fit in the viewport and reuses them while scrolling:
    /// the row that leaves at one edge comes back at the other edge with new data.
    /// After the last item the first one comes again, in both directions, so a short list never leaves empty space.
    /// Goes on the viewport object, together with a RectMask2D and a transparent Image that catches drags.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class RecyclingScrollView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler
    {
        // Below this speed (canvas units per second) a fling counts as stopped.
        private const float MinFlingSpeed = 1f;

        // How fast the measured drag speed follows the pointer. Smooths out one jittery frame before a release.
        private const float DragSpeedSmoothing = 10f;

        [SerializeField] private RecyclingScrollCell cellPrefab;

        [Header("Layout")]
        [SerializeField, Min(1)] private int columns = 2;
        [SerializeField] private Vector2 cellSize = new(160f, 160f);
        [SerializeField] private Vector2 spacing = new(12f, 12f);

        [Header("Scrolling")]
        [Tooltip("Distance moved by one mouse wheel notch, in canvas units.")]
        [SerializeField, Min(1f)] private float wheelStep = 80f;
        [Tooltip("Share of the fling speed left after one second. Same meaning as ScrollRect's deceleration rate.")]
        [SerializeField, Range(0f, 1f)] private float decelerationRate = 0.135f;

        private readonly List<RecyclingScrollCell> _cells = new();
        private readonly List<int> _boundIndices = new();
        private RectTransform _viewport;
        private Action<RecyclingScrollCell> _onCellCreated;
        private Action<RecyclingScrollCell, int> _bindCell;
        private int _itemCount;
        private int _pooledRowCount;
        private Vector2 _builtForViewportSize;

        // Distance scrolled down from the top of row 0, wrapped into one cycle.
        private float _offset;
        private float _velocity;
        private float _dragDistanceThisFrame;
        private bool _isDragging;
        private bool _isDirty;

        private float RowHeight => cellSize.y + spacing.y;

        private void Awake()
        {
            _viewport = (RectTransform)transform;
        }

        /// <param name="onCellCreated">Called once per pooled cell, e.g. to hook up its click event.</param>
        /// <param name="bindCell">Called when a cell starts showing another item. Gets the item index.</param>
        public void Initialize<TCell>(int itemCount, Action<TCell> onCellCreated, Action<TCell, int> bindCell)
            where TCell : RecyclingScrollCell
        {
            if (cellPrefab is not TCell)
                throw new InvalidOperationException($"{name}: cell prefab must have a {typeof(TCell).Name} component.");

            // Old cells were set up by the old callbacks, so they can't be reused.
            foreach (var cell in _cells)
                Destroy(cell.gameObject);

            _cells.Clear();
            _boundIndices.Clear();

            _itemCount = Mathf.Max(itemCount, 0);
            _onCellCreated = cell => onCellCreated((TCell)cell);
            _bindCell = (cell, index) => bindCell((TCell)cell, index);
            _offset = 0f;
            _velocity = 0f;

            BuildPool();
            Refresh();
        }

        private void LateUpdate()
        {
            if (_bindCell == null || _itemCount == 0)
                return;

            // The canvas scaler resizes the viewport when the window changes size.
            if (_viewport.rect.size != _builtForViewportSize)
                BuildPool();

            var deltaTime = Time.unscaledDeltaTime;
            if (_isDragging)
            {
                if (deltaTime > 0f)
                    _velocity = Mathf.Lerp(_velocity, _dragDistanceThisFrame / deltaTime, deltaTime * DragSpeedSmoothing);

                _dragDistanceThisFrame = 0f;
            }
            else if (_velocity != 0f)
            {
                _velocity *= Mathf.Pow(decelerationRate, deltaTime);
                if (Mathf.Abs(_velocity) < MinFlingSpeed)
                    _velocity = 0f;

                ScrollBy(_velocity * deltaTime);
            }

            // Only touch the cells when something moved; changing a RectTransform makes the canvas rebuild.
            if (_isDirty)
                Refresh();
        }

        private void OnDisable()
        {
            _isDragging = false;
            _velocity = 0f;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            _isDragging = true;
            _velocity = 0f;
            _dragDistanceThisFrame = 0f;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_isDragging)
                return;

            // Converted to canvas units, so dragging moves the cells exactly with the pointer at any screen size.
            var eventCamera = eventData.pressEventCamera;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_viewport, eventData.position, eventCamera, out var current) ||
                !RectTransformUtility.ScreenPointToLocalPointInRectangle(_viewport, eventData.position - eventData.delta, eventCamera, out var previous))
                return;

            var distance = current.y - previous.y;
            _dragDistanceThisFrame += distance;
            ScrollBy(distance);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
                _isDragging = false;
        }

        public void OnScroll(PointerEventData eventData)
        {
            // Wheel values differ between platforms, so only the direction is used: one notch, one step.
            if (Mathf.Approximately(eventData.scrollDelta.y, 0f))
                return;

            _velocity = 0f;
            ScrollBy(-Mathf.Sign(eventData.scrollDelta.y) * wheelStep);
        }

        private void ScrollBy(float distance)
        {
            if (distance == 0f || _itemCount == 0)
                return;

            // Wrapping by one full cycle looks the same, and the offset never grows big enough to lose float precision.
            var cycleHeight = RecyclingScrollMath.GetCycleRowCount(_itemCount, columns) * RowHeight;
            _offset = Mathf.Repeat(_offset + distance, cycleHeight);
            _isDirty = true;
        }

        private void BuildPool()
        {
            _builtForViewportSize = _viewport.rect.size;
            _pooledRowCount = _itemCount > 0 ? RecyclingScrollMath.GetPooledRowCount(_builtForViewportSize.y, RowHeight) : 0;
            var cellCount = _pooledRowCount * columns;

            while (_cells.Count < cellCount)
                CreateCell();

            // Spare cells from a taller window are kept for when it grows again.
            for (var i = 0; i < _cells.Count; i++)
            {
                _cells[i].gameObject.SetActive(i < cellCount);

                // Rows map to other cells after a resize, so every cell gets bound again.
                _boundIndices[i] = -1;
            }

            _isDirty = true;
        }

        private void CreateCell()
        {
            var cell = Instantiate(cellPrefab, _viewport);
            var rectTransform = cell.RectTransform;
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(0f, 1f);
            rectTransform.pivot = new Vector2(0f, 1f);
            rectTransform.sizeDelta = cellSize;

            _cells.Add(cell);
            _boundIndices.Add(-1);
            _onCellCreated(cell);
        }

        private void Refresh()
        {
            _isDirty = false;
            if (_pooledRowCount == 0)
                return;

            var gridWidth = columns * cellSize.x + (columns - 1) * spacing.x;
            var left = (_viewport.rect.width - gridWidth) * 0.5f;
            var firstRow = RecyclingScrollMath.GetFirstVisibleRow(_offset, RowHeight);

            for (var row = firstRow; row < firstRow + _pooledRowCount; row++)
            {
                // A pooled row always shows the rows with the same remainder, so the row that scrolls out
                // at the top is the one that comes back at the bottom.
                var poolRow = RecyclingScrollMath.PositiveModulo(row, _pooledRowCount);
                var top = _offset - row * RowHeight;

                for (var column = 0; column < columns; column++)
                {
                    var cellIndex = poolRow * columns + column;
                    var cell = _cells[cellIndex];
                    cell.RectTransform.anchoredPosition = new Vector2(left + column * (cellSize.x + spacing.x), top);

                    var dataIndex = RecyclingScrollMath.GetDataIndex(row, column, columns, _itemCount);
                    if (_boundIndices[cellIndex] == dataIndex)
                        continue;

                    _boundIndices[cellIndex] = dataIndex;
                    _bindCell(cell, dataIndex);
                }
            }
        }
    }
}
