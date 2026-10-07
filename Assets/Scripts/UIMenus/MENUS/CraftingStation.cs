using UnityEngine;

public class CraftingStation : Interactable
{
    [Tooltip("Menu de crafting. Se vazio, procura um na cena.")]
    [SerializeField]
    private CraftingMenu _menu;

    [SerializeField]
    private Inventory _secondInventory;

    [Tooltip("Receitas desta mesa. Se vazio, usa as receitas do CraftingMenu.")]
    [SerializeField]
    private Recipe[] _recipes;

    protected override string DefaultPrompt => "craft";

    protected override void OnInteract(GameObject interactor)
    {
        if (_menu == null)
            _menu = FindFirstObjectByType<CraftingMenu>();

        if (_menu == null)
        {
            Debug.LogError($"{name}: não existe nenhum CraftingMenu na cena.", this);
            return;
        }

        IInventory inventory = interactor.GetComponentInParent<IInventory>();
        if (inventory == null)
            inventory = interactor.GetComponentInChildren<IInventory>();

        if (inventory == null)
        {
            Debug.LogError($"{name}: o objeto que interagiu não tem IInventory.", this);
            return;
        }

        if (_secondInventory != null)
        {
            IInventory second = _secondInventory as IInventory;

            if (second == null)
                Debug.LogError($"{name}: o segundo inventário não implementa IInventory.", this);
            else if (!ReferenceEquals(second, inventory))
                inventory = new CompositeInventory(inventory, second);
        }

        _menu.Open(inventory, _recipes != null && _recipes.Length > 0 ? _recipes : null);
    }
}
