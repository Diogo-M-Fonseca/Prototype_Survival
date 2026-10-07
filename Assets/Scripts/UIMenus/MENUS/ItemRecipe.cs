using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

[CreateAssetMenu(menuName = "Item Recipe", fileName = "NewItemRecipe")]
public class ItemRecipe : Recipe, IRecipeDisplay
{
    [Serializable]
    public struct Entry
    {
        public Item item;

        [Min(1)]
        public int amount;
    }

    [SerializeField]
    private string _displayName;

    [SerializeField, TextArea]
    private string _description;

    [SerializeField]
    private Entry[] _ingredients;

    [SerializeField]
    private Entry[] _results;

    public string DisplayName
    {
        get
        {
            if (!string.IsNullOrEmpty(_displayName))
                return _displayName;
            if (_results != null && _results.Length > 0 && _results[0].item != null)
                return _results[0].item.DisplayName;
            return name;
        }
    }

    public Sprite Icon =>
        _results != null && _results.Length > 0 && _results[0].item != null
            ? _results[0].item.Icon
            : null;

    public override bool CanCraft(IInventory inventory)
    {
        if (inventory == null || _ingredients == null)
            return false;

        foreach (Entry e in _ingredients)
            if (IsValid(e) && RecipeInventory.Count(inventory, e.item) < e.amount)
                return false;

        return true;
    }

    protected override bool CanProduce(IInventory inventory)
    {
        if (_results == null)
            return true;

        foreach (Entry e in _results)
            if (IsValid(e) && !RecipeInventory.CanAdd(inventory, e.item, e.amount))
                return false;

        return true;
    }

    protected override bool Consume(IInventory inventory)
    {
        if (_ingredients == null)
            return true;

        List<Entry> removed = new List<Entry>();

        foreach (Entry e in _ingredients)
        {
            if (!IsValid(e))
                continue;

            if (!RecipeInventory.Remove(inventory, e.item, e.amount))
            {
                AddAll(inventory, removed); // desfaz o que já foi removido
                return false;
            }

            removed.Add(e);
        }

        return true;
    }

    protected override bool Produce(IInventory inventory)
    {
        if (_results == null)
            return true;

        List<Entry> added = new List<Entry>();

        foreach (Entry e in _results)
        {
            if (!IsValid(e))
                continue;

            int leftover = RecipeInventory.Add(inventory, e.item, e.amount);
            int done = e.amount - leftover;

            if (done > 0)
                added.Add(new Entry { item = e.item, amount = done });

            if (leftover > 0)
            {
                // Não coube tudo: retira o que foi adicionado e falha.
                foreach (Entry a in added)
                    RecipeInventory.Remove(inventory, a.item, a.amount);
                return false;
            }
        }

        return true;
    }

    protected override void Refund(IInventory inventory)
    {
        if (_ingredients != null)
            AddAll(inventory, _ingredients);
    }

    public string GetDetails(IInventory inventory)
    {
        StringBuilder sb = new StringBuilder();

        if (!string.IsNullOrEmpty(_description))
            sb.AppendLine(_description);

        if (_ingredients != null)
        {
            foreach (Entry e in _ingredients)
            {
                if (e.item == null)
                    continue;

                int have = inventory != null ? RecipeInventory.Count(inventory, e.item) : 0;
                string color = have >= e.amount ? "B0B0B0" : "C96A6A";
                sb.AppendLine($"<color=#{color}>{e.item.DisplayName}  {have}/{e.amount}</color>");
            }
        }

        return sb.ToString().TrimEnd();
    }

    private static bool IsValid(Entry e) => e.item != null && e.amount > 0;

    private static void AddAll(IInventory inventory, IEnumerable<Entry> entries)
    {
        foreach (Entry e in entries)
            if (IsValid(e))
                RecipeInventory.Add(inventory, e.item, e.amount);
    }
}
