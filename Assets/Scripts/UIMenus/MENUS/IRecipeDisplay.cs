using UnityEngine;

public interface IRecipeDisplay
{
    string DisplayName { get; }

    Sprite Icon { get; }

    string GetDetails(IInventory inventory);
}
