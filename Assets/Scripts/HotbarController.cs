using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class HotbarController : MonoBehaviour
{
    [SerializeField] private Inventory inventory;   // Hands
    [SerializeField] private InventoryGridUI hotbarUI;
    [SerializeField] private InventoryToggle bagToggle;

    private InputAction selectAction;
    private InputAction useAction;
    private int selected = -1; // -1 = mão vazia

    public int Selected => selected;
    public event Action<int> SelectionChanged;

    private void Awake()
    {
        selectAction = InputSystem.actions.FindAction("Player/HotbarSelect", true);
        useAction = InputSystem.actions.FindAction("Player/Attack", true);
    }

    private void OnEnable()
    {
        selectAction.performed += OnSelect;
        useAction.performed += OnUse;
    }

    private void OnDisable()
    {
        selectAction.performed -= OnSelect;
        useAction.performed -= OnUse;
    }

    private void Start() => Select(-1);

    private void OnSelect(InputAction.CallbackContext ctx)
    {
        int index = ctx.action.GetBindingIndexForControl(ctx.control);
        if (index < 0 || index >= hotbarUI.SlotCount) return;

        Select(selected == index ? -1 : index); // mesmo número desequipa
    }

    private void OnUse(InputAction.CallbackContext ctx)
    {
        if (bagToggle.IsOpen || selected < 0) return;
        inventory.UseSlot(selected, gameObject);
    }

    private void Select(int index)
    {
        selected = index;
        hotbarUI.SetSelected(selected);
        SelectionChanged?.Invoke(selected);
    }
}
