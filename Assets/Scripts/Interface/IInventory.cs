using System;

public interface IInventory 
{
    event Action Changed;
    int Add(Item item, int amount);
    bool CanAdd(Item item, int amount);
    bool Remove(Item item, int amount);
    int Count(Item item);
    bool Has(Item item, int amount);
}
