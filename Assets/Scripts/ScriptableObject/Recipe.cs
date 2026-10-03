using UnityEngine;

public abstract class Recipe : ScriptableObject
{
    public abstract bool CanCraft(IInventory inventory);
    protected abstract bool CanProduce(IInventory inventory);
    protected abstract void Consume(IInventory inventory);
    protected abstract void Produce(IInventory inventory);

    public bool Craft(IInventory inventory)
    {
        if (!CanCraft(inventory) || !CanProduce(inventory)) return false;
        Consume(inventory);
        Produce(inventory);
        return true;
    }
}