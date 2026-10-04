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
    private int selected = -1; // -1 = m�o vazia

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

    private void Update()
    {
        if (bagToggle != null && bagToggle.IsOpen) return;
        if (Mouse.current == null || hotbarUI.SlotCount == 0) return;

        float scroll = Mouse.current.scroll.ReadValue().y;
        if (Mathf.Approximately(scroll, 0f)) return;

        int direction = scroll > 0f ? 1 : -1;
        int next = selected < 0
            ? (direction > 0 ? 0 : hotbarUI.SlotCount - 1)
            : (selected + direction + hotbarUI.SlotCount) % hotbarUI.SlotCount;

        Select(next);
    }

    private void OnSelect(InputAction.CallbackContext ctx)
    {
        int index = ctx.action.GetBindingIndexForControl(ctx.control);
        if (index < 0 || index >= hotbarUI.SlotCount) return;

        Select(selected == index ? -1 : index); // mesmo n�mero desequipa
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
