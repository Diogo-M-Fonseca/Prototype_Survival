using UnityEngine;

public class CraftingStation : Interactable
{
    [Tooltip("Menu de crafting. Se vazio, procura um na cena.")]
    [SerializeField]
    private CraftingMenu _menu;

    [Tooltip("Mochila do jogador, aberta junto com o menu. Se vazio, procura uma na cena.")]
    [SerializeField]
    private InventoryToggle _bagToggle;

    [SerializeField]
    private Inventory _secondInventory;

    [Tooltip("Receitas desta mesa. Se vazio, usa as receitas do CraftingMenu.")]
    [SerializeField]
    private Recipe[] _recipes;

    private bool _hooked;

    protected override string DefaultPrompt => "craft";

    private void OnDisable()
    {
        Unhook();
    }

    protected override void OnInteract(GameObject interactor)
    {
        if (_menu == null)
            _menu = FindFirstObjectByType<CraftingMenu>();

        if (_menu == null)
        {
            Debug.LogError($"{name}: não existe nenhum CraftingMenu na cena.", this);
            return;
        }

        if (_bagToggle == null)
            _bagToggle = FindFirstObjectByType<InventoryToggle>();

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

        // Com a mochila presente, é ela quem gere o cursor (evita dois donos a restaurar estados diferentes).
        _menu.ManageCursor = _bagToggle == null;

        Hook();

        _menu.Open(inventory, _recipes != null && _recipes.Length > 0 ? _recipes : null);

        if (_bagToggle != null && !_bagToggle.IsOpen)
            _bagToggle.Open();
    }

    private void Hook()
    {
        if (_hooked) return;
        _hooked = true;

        _menu.VisibilityChanged += OnMenuVisibilityChanged;
        if (_bagToggle != null) _bagToggle.Toggled += OnBagToggled;
    }

    private void Unhook()
    {
        if (!_hooked) return;
        _hooked = false;

        if (_menu != null) _menu.VisibilityChanged -= OnMenuVisibilityChanged;
        if (_bagToggle != null) _bagToggle.Toggled -= OnBagToggled;
    }

    // Menu de crafting fechou -> fecha a mochila.
    private void OnMenuVisibilityChanged(bool open)
    {
        if (open) return;

        Unhook();
        if (_bagToggle != null && _bagToggle.IsOpen)
            _bagToggle.Close();
    }

    // Mochila fechou (ex.: tecla da mochila) -> fecha o menu de crafting.
    private void OnBagToggled(bool open)
    {
        if (open) return;

        Unhook();
        if (_menu != null && _menu.IsOpen)
            _menu.Close();
    }
}
