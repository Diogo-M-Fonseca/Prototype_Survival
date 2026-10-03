using System;
using UnityEngine;

public class InventoryGridUI : MonoBehaviour
{
    [SerializeField] private Inventory inventory;
    [SerializeField] private SlotUI slotPrefab;
    [SerializeField] private Transform container;
    [SerializeField, Min(1)] private int slotCount = 9;

    private SlotUI[] slotUIs;

    public event Action<int> SlotClicked;
    public int SlotCount => slotUIs != null ? slotUIs.Length : 0;

    private void Awake()
    {
        int count = Mathf.Min(slotCount, inventory.Size);
        slotUIs = new SlotUI[count];

        for (int i = 0; i < count; i++)
        {
            slotUIs[i] = Instantiate(slotPrefab, container);
            slotUIs[i].Init(i, index => SlotClicked?.Invoke(index));
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
}
