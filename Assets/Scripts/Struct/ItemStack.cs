using System;
using UnityEngine;

[Serializable]
public struct ItemStack
{
    [SerializeField] private Item item;
    [SerializeField] private int amount;
    [SerializeField] private float durabilityRemaining;

    public ItemStack(Item item, int amount)
    {
        this.item = item;
        this.amount = amount;
        durabilityRemaining = item != null && item.HasDurability
            ? item.MaxDurability
            : -1f;
    }

    public Item Item => item;
    public int Amount => amount;
    public float DurabilityRemaining => durabilityRemaining;
    public bool IsEmpty => item == null || amount <= 0;

    public ItemStack WithAmount(int newAmount) => new ItemStack(item, newAmount);

    public ItemStack WithDurability(float newDurability)
    {
        ItemStack result = this;
        result.durabilityRemaining = newDurability;
        return result;
    }
}
