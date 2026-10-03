using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Item Recipe")]
public class ItemRecipe : Recipe
{
    [SerializeField] private ItemStack[] ingredients;
    [SerializeField] private ItemStack result;

    public override bool CanCraft(IInventory inventory)
    {
        Dictionary<Item, int> needed = new Dictionary<Item, int>();
        foreach (ItemStack i in ingredients)
        {
            if (i.IsEmpty) continue;
            needed.TryGetValue(i.Item, out int current);
            needed[i.Item] = current + i.Amount;
        }

        foreach (KeyValuePair<Item, int> pair in needed)
            if (!inventory.Has(pair.Key, pair.Value)) return false;

        return true;
    }

    protected override bool CanProduce(IInventory inventory)
        => result.IsEmpty || inventory.CanAdd(result.Item, result.Amount);

    protected override void Consume(IInventory inventory)
    {
        foreach (ItemStack i in ingredients)
            if (!i.IsEmpty) inventory.Remove(i.Item, i.Amount);
    }

    protected override void Produce(IInventory inventory)
    {
        if (!result.IsEmpty) inventory.Add(result.Item, result.Amount);
    }
}