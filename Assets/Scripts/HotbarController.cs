using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class HotbarController : MonoBehaviour
{
    [SerializeField] private Inventory _inventory;
    [SerializeField] private InventoryGridUI _hotbarUI;
    [SerializeField] private InventoryToggle _bagToggle;

    private InputAction _selectAction;
    private InputAction _useAction;
    private int _selected = -1;

    public int Selected => _selected;
    public event Action<int> SelectionChanged;

    private void Awake()
    {
        _selectAction = InputSystem.actions.FindAction("Player/HotbarSelect", true);
        _useAction = InputSystem.actions.FindAction("Player/Attack", true);
    }

    private void OnEnable()
    {
        _selectAction.performed += OnSelect;
        _useAction.performed += OnUse;
    }

    private void OnDisable()
    {
        _selectAction.performed -= OnSelect;
        _useAction.performed -= OnUse;
    }

    private void Start() => Select(-1);

    private void Update()
    {
        if (_bagToggle != null && _bagToggle.IsOpen) return;
        if (Mouse.current == null || _hotbarUI.SlotCount == 0) return;

        float scroll = Mouse.current.scroll.ReadValue().y;
        if (Mathf.Approximately(scroll, 0f)) return;

        int direction = scroll > 0f ? 1 : -1;
        int next = _selected < 0
            ? (direction > 0 ? 0 : _hotbarUI.SlotCount - 1)
            : (_selected + direction + _hotbarUI.SlotCount) % _hotbarUI.SlotCount;

        Select(next);
    }

    private void OnSelect(InputAction.CallbackContext ctx)
    {
        int index = ctx.action.GetBindingIndexForControl(ctx.control);
        if (index < 0 || index >= _hotbarUI.SlotCount) return;

        Select(_selected == index ? -1 : index);
    }

    private void OnUse(InputAction.CallbackContext ctx)
    {
        if (_bagToggle.IsOpen || _selected < 0) return;
        _inventory.UseSlot(_selected, gameObject);
    }

    private void Select(int index)
    {
        _selected = index;
        _hotbarUI.SetSelected(_selected);
        SelectionChanged?.Invoke(_selected);
    }
}
