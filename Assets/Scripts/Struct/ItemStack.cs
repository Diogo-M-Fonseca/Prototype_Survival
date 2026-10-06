using System;
using UnityEngine;

[Serializable]
public struct ItemStack
{
    [SerializeField] private Item _item;
    [SerializeField] private int _amount;
    [SerializeField] private float _durabilityRemaining;

    public ItemStack(Item item, int amount)
    {
        _item = item;
        _amount = amount;
        _durabilityRemaining = item != null && item.HasDurability
            ? item.MaxDurability
            : -1f;
    }

    public Item Item => _item;
    public int Amount => _amount;
    public float DurabilityRemaining => _durabilityRemaining;
    public bool IsEmpty => _item == null || _amount <= 0;

    public ItemStack WithAmount(int newAmount)
    {
        ItemStack result = this;
        result._amount = newAmount;
        return result;
    }

    public ItemStack WithDurability(float newDurability)
    {
        ItemStack result = this;
        result._durabilityRemaining = newDurability;
        return result;
    }
}
