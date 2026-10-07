using System;

public class CompositeInventory : IInventory
{
    private readonly IInventory[] _inventories;

    public CompositeInventory(params IInventory[] inventories)
    {
        _inventories = Array.FindAll(inventories ?? new IInventory[0], i => i != null);
    }

    public event Action Changed
    {
        add { foreach (IInventory inv in _inventories) inv.Changed += value; }
        remove { foreach (IInventory inv in _inventories) inv.Changed -= value; }
    }

    public int Count(Item item)
    {
        int total = 0;
        foreach (IInventory inv in _inventories) total += inv.Count(item);
        return total;
    }

    public bool Has(Item item, int amount) => Count(item) >= amount;

    public bool CanAdd(Item item, int amount)
    {
        foreach (IInventory inv in _inventories)
            if (inv.CanAdd(item, amount)) return true;

        return false;
    }

    public int Add(Item item, int amount)
    {
        int leftover = amount;

        foreach (IInventory inv in _inventories)
        {
            if (leftover <= 0) break;
            leftover = inv.Add(item, leftover);
        }

        return leftover;
    }

    public bool Remove(Item item, int amount)
    {
        if (Count(item) < amount) return false;

        int remaining = amount;

        foreach (IInventory inv in _inventories)
        {
            if (remaining <= 0) break;

            int take = Math.Min(remaining, inv.Count(item));
            if (take <= 0) continue;

            if (!inv.Remove(item, take)) return false;
            remaining -= take;
        }

        return remaining <= 0;
    }
}
