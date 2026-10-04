using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class InventoryGridUI : MonoBehaviour
{
    [SerializeField] private Inventory inventory;
    [SerializeField] private SlotUI slotPrefab;
    [SerializeField] private Transform container;
    [SerializeField, Min(1)] private int slotCount = 9;

    private SlotUI[] slotUIs;

    public Inventory Inventory => inventory;
    public int SlotCount => slotUIs != null ? slotUIs.Length : 0;

    public event Action<InventoryGridUI, int, PointerEventData> BeginDrag;
    public event Action<PointerEventData> Drag;
    public event Action EndDrag;
    public event Action<InventoryGridUI, int> Drop;

    private void Awake()
    {
        int count = Mathf.Min(slotCount, inventory.Size);
        slotUIs = new SlotUI[count];

        for (int i = 0; i < count; i++)
        {
            slotUIs[i] = Instantiate(slotPrefab, container);
            slotUIs[i].Init(this, i);
        }

        inventory.Changed += Refresh;
    }

    private void OnEnable() => Refresh();

    private void OnDestroy()
    {
        if (inventory != null) inventory.Changed -= Refresh;
    }

    public void Refresh()
    {
        if (slotUIs == null) return;
        for (int i = 0; i < slotUIs.Length; i++)
            slotUIs[i].Set(inventory[i]);
    }

    public void SetSelected(int index)
    {
        if (slotUIs == null) return;
        for (int i = 0; i < slotUIs.Length; i++)
            slotUIs[i].SetSelected(i == index);
    }

    public void SetMarked(int index)
    {
        if (slotUIs == null) return;
        for (int i = 0; i < slotUIs.Length; i++)
            slotUIs[i].SetMarked(i == index);
    }

    public void NotifyBeginDrag(int index, PointerEventData e) => BeginDrag?.Invoke(this, index, e);
    public void NotifyDrag(PointerEventData e) => Drag?.Invoke(e);
    public void NotifyEndDrag() => EndDrag?.Invoke();
    public void NotifyDrop(int index) => Drop?.Invoke(this, index);
}
