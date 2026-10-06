using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class InventoryGridUI : MonoBehaviour
{
    [SerializeField] private Inventory _inventory;
    [SerializeField] private SlotUI _slotPrefab;
    [SerializeField] private Transform _Grid;

    [SerializeField, Min(1)] private int _slotCount = 9;

    private readonly List<SlotUI> _slots = new List<SlotUI>();
    private int _count;

    public Inventory Inventory => _inventory;
    public int SlotCount => _count;

    public event Action<InventoryGridUI, int, PointerEventData> BeginDrag;
    public event Action<PointerEventData> Drag;
    public event Action EndDrag;
    public event Action<InventoryGridUI, int> Drop;

    private void Awake()
    {
        if (_inventory != null) Bind(_inventory);
    }

    private void OnEnable() => Refresh();

    private void OnDestroy()
    {
        if (_inventory != null) _inventory.Changed -= Refresh;
    }

    public void Bind(Inventory inventory)
    {
        if (_inventory != null) _inventory.Changed -= Refresh;

        _inventory = inventory;
        _count = _inventory != null ? Mathf.Min(_slotCount, _inventory.Size) : 0;

        for (int i = 0; i < _count; i++)
        {
            if (i >= _slots.Count)
                _slots.Add(Instantiate(_slotPrefab, _Grid));

            SlotUI slot = _slots[i];
            slot.gameObject.SetActive(true);
            slot.Init(this, i);
            slot.SetSelected(false);
            slot.SetMarked(false);
        }

        for (int i = _count; i < _slots.Count; i++)
            _slots[i].gameObject.SetActive(false);

        if (_inventory == null) return;

        _inventory.Changed += Refresh;
        Refresh();
    }

    public void Unbind()
    {
        Bind(null);
    }

    public void Refresh()
    {
        if (_slots == null) return;
        for (int i = 0; i < _count; i++)
            _slots[i].Set(_inventory[i]);
    }

    public void SetSelected(int index)
    {
        for (int i = 0; i < _count; i++)
            _slots[i].SetSelected(i == index);
    }

    public void SetMarked(int index)
    {
        for (int i = 0; i < _count; i++)
            _slots[i].SetMarked(i == index);
    }

    public void NotifyBeginDrag(int index, PointerEventData e) => BeginDrag?.Invoke(this, index, e);
    public void NotifyDrag(PointerEventData e) => Drag?.Invoke(e);
    public void NotifyEndDrag() => EndDrag?.Invoke();
    public void NotifyDrop(int index) => Drop?.Invoke(this, index);
}
