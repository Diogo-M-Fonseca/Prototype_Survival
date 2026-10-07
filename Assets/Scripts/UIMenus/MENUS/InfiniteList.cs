using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class InfiniteList
    : MonoBehaviour,
        IScrollHandler,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler
{
    private const int PoolExtra = 3;

    [Header("Referências")]
    [SerializeField]
    private UIStyle _style;

    [SerializeField]
    private RectTransform _viewport;

    [SerializeField]
    private RectTransform _indicatorTrack;

    [SerializeField]
    private RectTransform _indicatorHandle;

    [Header("Layout")]
    [SerializeField, Min(1)]
    private int _visibleRows = 6;

    [Tooltip("Posição (0 = topo) onde fica a linha selecionada.")]
    [SerializeField, Min(0)]
    private int _selectedSlot = 2;

    [SerializeField]
    private bool _loop = true;

    [Header("Movimento")]
    [SerializeField, Min(0f)]
    private float _smoothTime = 0.08f;

    [Header("Entrada")]
    [SerializeField]
    private bool _keyboardInput = true;

    [SerializeField]
    private bool _wheelInput = true;

    [SerializeField]
    private bool _dragInput = true;

    [Header("Transparência por distância (em linhas)")]
    [SerializeField]
    private AnimationCurve _fadeAbove = new AnimationCurve(
        new Keyframe(0f, 1f),
        new Keyframe(1f, 1f),
        new Keyframe(3f, 0.6f)
    );

    [SerializeField]
    private AnimationCurve _fadeBelow = new AnimationCurve(
        new Keyframe(0f, 1f),
        new Keyframe(1f, 0.55f),
        new Keyframe(2f, 0.35f),
        new Keyframe(3f, 0.1f)
    );

    private readonly List<ListRow> _pool = new List<ListRow>();

    private int _count;
    private Func<int, string> _label;
    private Func<int, bool> _available;
    private Func<int, Sprite> _icon;

    private float _position;
    private float _target;
    private float _velocity;
    private bool _dragging;
    private int _lastSelected = -1;

    public event Action<int> SelectionChanged;

    public event Action<int> Submitted;

    public bool InputEnabled { get; set; } = true;
    public RectTransform Root => (RectTransform)transform;
    public int Count => _count;
    public int SelectedIndex => _count == 0 ? -1 : Wrap(Mathf.RoundToInt(_target));
    public bool Loop
    {
        get => _loop;
        set
        {
            _loop = value;
            _target = ClampTarget(_target);
        }
    }

    public void Configure(
        UIStyle style,
        RectTransform viewport,
        int visibleRows,
        int selectedSlot,
        bool loop
    )
    {
        _style = style;
        _viewport = viewport;
        _visibleRows = Mathf.Max(1, visibleRows);
        _selectedSlot = Mathf.Clamp(selectedSlot, 0, _visibleRows - 1);
        _loop = loop;
        ClearPool();
    }

    public void SetIndicator(RectTransform track, RectTransform handle)
    {
        _indicatorTrack = track;
        _indicatorHandle = handle;
    }

    public void SetData(
        int count,
        Func<int, string> label,
        Func<int, bool> available = null,
        Func<int, Sprite> icon = null,
        int selectedIndex = 0
    )
    {
        _count = Mathf.Max(0, count);
        _label = label;
        _available = available;
        _icon = icon;

        EnsurePool();

        _position = _target = ClampTarget(selectedIndex);
        _velocity = 0f;
        _lastSelected = -1;
        Refresh();
        NotifySelection();
    }

    public void SetItems<T>(
        IReadOnlyList<T> items,
        Func<T, string> label,
        Func<T, bool> available = null,
        Func<T, Sprite> icon = null,
        int selectedIndex = 0
    )
    {
        SetData(
            items.Count,
            i => label(items[i]),
            available != null ? i => available(items[i]) : (Func<int, bool>)null,
            icon != null ? i => icon(items[i]) : (Func<int, Sprite>)null,
            selectedIndex
        );
    }

    public void Refresh()
    {
        foreach (ListRow row in _pool)
            row.BoundIndex = int.MinValue;
    }

    public void Select(int index, bool instant = false)
    {
        if (_count == 0)
            return;

        index = Mathf.Clamp(index, 0, _count - 1);
        int current = Wrap(Mathf.RoundToInt(_target));
        int diff = index - current;

        if (_loop)
        {
            if (diff > _count / 2)
                diff -= _count;
            else if (diff < -_count / 2)
                diff += _count;
        }

        _target = ClampTarget(Mathf.Round(_target) + diff);

        if (instant)
        {
            _position = _target;
            _velocity = 0f;
        }

        NotifySelection();
    }

    public void Next() => Move(1);

    public void Previous() => Move(-1);

    public void Move(int delta)
    {
        if (_count == 0)
            return;

        _target = ClampTarget(Mathf.Round(_target) + delta);
        NotifySelection();
    }

    public void Submit()
    {
        if (_count > 0)
            Submitted?.Invoke(SelectedIndex);
    }

    private void Update()
    {
        if (_pool.Count == 0 || _style == null)
            return;

        if (InputEnabled)
            HandleKeyboard();

        if (!_dragging)
            _position = Mathf.SmoothDamp(
                _position,
                _target,
                ref _velocity,
                _smoothTime,
                Mathf.Infinity,
                Time.unscaledDeltaTime
            );

        Layout();
        UpdateIndicator();
    }

    private void Layout()
    {
        float step = _style.RowStep;
        int baseIndex = Mathf.FloorToInt(_position) - _selectedSlot - 1;

        for (int k = 0; k < _pool.Count; k++)
        {
            ListRow row = _pool[k];
            int virtualIndex = baseIndex + k;
            bool valid = _count > 0 && (_loop || (virtualIndex >= 0 && virtualIndex < _count));

            if (row.gameObject.activeSelf != valid)
                row.gameObject.SetActive(valid);
            if (!valid)
                continue;

            float distance = virtualIndex - _position;
            row.Rect.anchoredPosition = new Vector2(0f, -(_selectedSlot + distance) * step);

            if (row.BoundIndex != virtualIndex)
            {
                row.BoundIndex = virtualIndex;
                BindRow(row, Wrap(virtualIndex));
            }

            float selection = 1f - Mathf.Clamp01(Mathf.Abs(distance));
            float alpha =
                distance < 0f ? _fadeAbove.Evaluate(-distance) : _fadeBelow.Evaluate(distance);
            row.ApplyVisual(selection, alpha);
        }
    }

    private void BindRow(ListRow row, int dataIndex)
    {
        string text = _label != null ? _label(dataIndex) : dataIndex.ToString();
        bool available = _available == null || _available(dataIndex);
        Sprite icon = _icon != null ? _icon(dataIndex) : null;
        row.Bind(text, icon, available);
    }

    private void UpdateIndicator()
    {
        if (_indicatorTrack == null || _indicatorHandle == null || _count == 0)
            return;

        float trackHeight = _indicatorTrack.rect.height;
        float handleHeight = Mathf.Clamp(trackHeight * _visibleRows / _count, 24f, trackHeight);
        float t = _loop
            ? Mathf.Repeat(_position, _count) / _count
            : (_count > 1 ? Mathf.Clamp01(_position / (_count - 1)) : 0f);

        _indicatorHandle.sizeDelta = new Vector2(_indicatorHandle.sizeDelta.x, handleHeight);
        _indicatorHandle.anchoredPosition = new Vector2(0f, -(trackHeight - handleHeight) * t);
    }

    private void HandleKeyboard()
    {
        if (!_keyboardInput || _count == 0)
            return;

#if ENABLE_INPUT_SYSTEM
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb == null)
            return;

        if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame)
            Move(-1);
        if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame)
            Move(1);
        if (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)
            Submit();
#elif ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            Move(-1);
        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            Move(1);
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
            Submit();
#endif
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (!InputEnabled || !_wheelInput || Mathf.Approximately(eventData.scrollDelta.y, 0f))
            return;

        Move(eventData.scrollDelta.y > 0f ? -1 : 1);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!InputEnabled || !_dragInput || _count == 0)
            return;

        _dragging = true;
        _velocity = 0f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_dragging)
            return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _viewport,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 current
        );
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _viewport,
            eventData.position - eventData.delta,
            eventData.pressEventCamera,
            out Vector2 previous
        );

        _position += (current.y - previous.y) / _style.RowStep;
        if (!_loop)
            _position = Mathf.Clamp(_position, -0.5f, _count - 0.5f);

        _target = _position;
        NotifySelection();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!_dragging)
            return;

        _dragging = false;
        _target = ClampTarget(Mathf.Round(_position));
        NotifySelection();
    }

    private void OnRowClicked(ListRow row)
    {
        if (!InputEnabled || _count == 0)
            return;

        if (row.BoundIndex == Mathf.RoundToInt(_target))
            Submit();
        else
        {
            _target = ClampTarget(row.BoundIndex);
            NotifySelection();
        }
    }

    private void NotifySelection()
    {
        int selected = SelectedIndex;
        if (selected == _lastSelected)
            return;

        _lastSelected = selected;
        SelectionChanged?.Invoke(selected);
    }

    private float ClampTarget(float value)
    {
        return _loop ? value : Mathf.Clamp(value, 0f, Mathf.Max(0, _count - 1));
    }

    private int Wrap(int virtualIndex)
    {
        if (_count == 0)
            return 0;
        return ((virtualIndex % _count) + _count) % _count;
    }

    private void EnsurePool()
    {
        if (_viewport == null)
            _viewport = Root;

        int needed = _visibleRows + PoolExtra;
        if (_pool.Count == needed)
            return;

        ClearPool();

        for (int i = 0; i < needed; i++)
        {
            ListRow row = UIFactory.CreateRow(_viewport, _style);
            row.Clicked += OnRowClicked;
            _pool.Add(row);
        }
    }

    private void ClearPool()
    {
        foreach (ListRow row in _pool)
            if (row != null)
                Destroy(row.gameObject);

        _pool.Clear();
    }
}
