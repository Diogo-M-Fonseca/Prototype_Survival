
public static class RecipeInventory
{

    public static int Count(IInventory inventory, Item item) => inventory.Count(item);

    public static bool CanAdd(IInventory inventory, Item item, int amount) =>
        inventory.CanAdd(item, amount);


    public static int Add(IInventory inventory, Item item, int amount) =>
        inventory.Add(item, amount);

    public static bool Remove(IInventory inventory, Item item, int amount) =>
        inventory.Remove(item, amount);
}
