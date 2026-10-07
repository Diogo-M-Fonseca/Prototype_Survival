using UnityEngine;

public abstract class Recipe : ScriptableObject
{
    public abstract bool CanCraft(IInventory inventory);
    protected abstract bool CanProduce(IInventory inventory);

    /// <summary>
    /// Consome os ingredientes. Tudo ou nada: se devolver false, o inventário fica como estava.
    /// </summary>
    protected abstract bool Consume(IInventory inventory);

    /// <summary>
    /// Produz os resultados. Tudo ou nada: se devolver false, o inventário fica como estava.
    /// </summary>
    protected abstract bool Produce(IInventory inventory);

    /// <summary>
    /// Devolve os ingredientes quando a produção falha depois de consumir.
    /// </summary>
    protected abstract void Refund(IInventory inventory);

    public bool Craft(IInventory inventory)
    {
        if (!CanCraft(inventory) || !CanProduce(inventory)) return false;
        if (!Consume(inventory)) return false;

        if (Produce(inventory)) return true;

        Refund(inventory);
        return false;
    }
}
