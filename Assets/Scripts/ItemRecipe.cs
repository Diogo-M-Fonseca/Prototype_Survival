using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Item Recipe")]
public class ItemRecipe : Recipe
{
    [SerializeField] private ItemStack[] _ingredients;
    [SerializeField] private ItemStack _result;

    public override bool CanCraft(IInventory inventory)
    {
        Dictionary<Item, int> needed = new Dictionary<Item, int>();
        foreach (ItemStack i in _ingredients)
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
        => _result.IsEmpty || inventory.CanAdd(_result.Item, _result.Amount);

    protected override void Consume(IInventory inventory)
    {
        foreach (ItemStack i in _ingredients)
            if (!i.IsEmpty) inventory.Remove(i.Item, i.Amount);
    }

    protected override void Produce(IInventory inventory)
    {
        if (!_result.IsEmpty) inventory.Add(_result.Item, _result.Amount);
    }
}