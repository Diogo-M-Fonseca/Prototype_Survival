using System;
using UnityEngine;

[Serializable]
public struct ItemStack
{
    [SerializeField] private Item item;
    [SerializeField] private int amount;

    public ItemStack(Item item, int amount)
    {
        this.item = item;
        this.amount = amount;
    }

    public Item Item => item;
    public int Amount => amount;
    public bool IsEmpty => item == null || amount <= 0;

    public ItemStack WithAmount(int newAmount) => new ItemStack(item, newAmount);
}
