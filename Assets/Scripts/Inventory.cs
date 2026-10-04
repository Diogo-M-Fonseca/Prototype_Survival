using System;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class Inventory : MonoBehaviour, IInventory
{
    [SerializeField, Min(1)] private int size = 20;

    private ItemStack[] slots;

    public event Action Changed;

    public int Size => size;
    public ItemStack this[int i] => slots[i];

    private void Awake() => slots = new ItemStack[size];

    public int Add(Item item, int amount)
    {
        if (item == null || amount <= 0) return amount;

        int remaining = amount;

        for (int i = 0; i < slots.Length && remaining > 0; i++)
        {
            if (slots[i].IsEmpty || slots[i].Item != item) continue;

            int add = Mathf.Min(remaining, item.MaxStack - slots[i].Amount);
            if (add <= 0) continue;

            slots[i] = slots[i].WithAmount(slots[i].Amount + add);
            remaining -= add;
        }

        for (int i = 0; i < slots.Length && remaining > 0; i++)
        {
            if (!slots[i].IsEmpty) continue;

            int add = Mathf.Min(remaining, item.MaxStack);
            slots[i] = new ItemStack(item, add);
            remaining -= add;
        }

        if (remaining != amount) Changed?.Invoke();
        return remaining;
    }

    public bool CanAdd(Item item, int amount)
    {
        if (item == null) return false;

        foreach (ItemStack s in slots)
        {
            if (amount <= 0) break;

            if (s.IsEmpty) amount -= item.MaxStack;
            else if (s.Item == item) amount -= item.MaxStack - s.Amount;
        }
        return amount <= 0;
    }

    public int Count(Item item)
    {
        if (item == null) return 0;

        int total = 0;
        foreach (ItemStack s in slots)
            if (!s.IsEmpty && s.Item == item) total += s.Amount;
        return total;
    }

    public bool Has(Item item, int amount) => Count(item) >= amount;

    public bool Remove(Item item, int amount)
    {
        if (item == null || amount <= 0 || !Has(item, amount)) return false;

        for (int i = slots.Length - 1; i >= 0 && amount > 0; i--)
        {
            if (slots[i].IsEmpty || slots[i].Item != item) continue;

            int take = Mathf.Min(amount, slots[i].Amount);
            int left = slots[i].Amount - take;
            slots[i] = left <= 0 ? default : slots[i].WithAmount(left);
            amount -= take;
        }

        Changed?.Invoke();
        return true;
    }

    public bool UseSlot(int index, GameObject user)
    {
        if ((uint)index >= (uint)slots.Length) return false;

        ItemStack slot = slots[index];
        if (slot.IsEmpty || !slot.Item.Use(user)) return false;

        int left = slot.Amount - 1;
        slots[index] = left <= 0 ? default : slot.WithAmount(left);

        Changed?.Invoke();
        return true;
    }

    public bool TransferSlot(int index, Inventory target)
    {
        if (target == null || target == this) return false;
        if ((uint)index >= (uint)slots.Length) return false;

        ItemStack s = slots[index];
        if (s.IsEmpty) return false;

        int left = target.Add(s.Item, s.Amount);
        if (left == s.Amount) return false;

        slots[index] = left <= 0 ? default : s.WithAmount(left);
        Changed?.Invoke();
        return true;
    }

    public bool MoveOrSwap(int from, Inventory target, int to)
    {
        if (target == null) return false;
        if ((uint)from >= (uint)slots.Length || (uint)to >= (uint)target.slots.Length) return false;
        if (target == this && from == to) return false;

        ItemStack a = slots[from];
        ItemStack b = target.slots[to];
        if (a.IsEmpty) return false;

        if (!b.IsEmpty && b.Item == a.Item)
        {
            int space = a.Item.MaxStack - b.Amount;
            if (space > 0)
            {
                int move = Mathf.Min(space, a.Amount);
                target.slots[to] = b.WithAmount(b.Amount + move);

                int left = a.Amount - move;
                slots[from] = left <= 0 ? default : a.WithAmount(left);

                Changed?.Invoke();
                if (target != this) target.Changed?.Invoke();
                return true;
            }
        }

        slots[from] = b;
        target.slots[to] = a;

        Changed?.Invoke();
        if (target != this) target.Changed?.Invoke();
        return true;
    }
}