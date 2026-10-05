using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class InventoryGridUI : MonoBehaviour
{
    [SerializeField] private Inventory _inventory;
    [SerializeField] private SlotUI _slotPrefab;
    [SerializeField] private Transform _Grid;
    [SerializeField, Min(1)] private int _slotCount = 9;

    private SlotUI[] _slotUIs;

    public Inventory Inventory => _inventory;
    public int SlotCount => _slotUIs != null ? _slotUIs.Length : 0;

    public event Action<InventoryGridUI, int, PointerEventData> BeginDrag;
    public event Action<PointerEventData> Drag;
    public event Action EndDrag;
    public event Action<InventoryGridUI, int> Drop;

    private void Awake()
    {
        int count = Mathf.Min(_slotCount, _inventory.Size);
        _slotUIs = new SlotUI[count];

        for (int i = 0; i < count; i++)
        {
            _slotUIs[i] = Instantiate(_slotPrefab, _Grid);
            _slotUIs[i].Init(this, i);
        }

        _inventory.Changed += Refresh;
    }

    private void OnEnable() => Refresh();

    private void OnDestroy()
    {
        if (_inventory != null) _inventory.Changed -= Refresh;
    }

    public void Refresh()
    {
        if (_slotUIs == null) return;
        for (int i = 0; i < _slotUIs.Length; i++)
            _slotUIs[i].Set(_inventory[i]);
    }

    public void SetSelected(int index)
    {
        if (_slotUIs == null) return;
        for (int i = 0; i < _slotUIs.Length; i++)
            _slotUIs[i].SetSelected(i == index);
    }

    public void SetMarked(int index)
    {
        if (_slotUIs == null) return;
        for (int i = 0; i < _slotUIs.Length; i++)
            _slotUIs[i].SetMarked(i == index);
    }

    public void NotifyBeginDrag(int index, PointerEventData e) => BeginDrag?.Invoke(this, index, e);
    public void NotifyDrag(PointerEventData e) => Drag?.Invoke(e);
    public void NotifyEndDrag() => EndDrag?.Invoke();
    public void NotifyDrop(int index) => Drop?.Invoke(this, index);
}
