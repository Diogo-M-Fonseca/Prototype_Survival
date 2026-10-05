using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InventoryToggle : MonoBehaviour
{
    [SerializeField] private GameObject _panel;

    private InputAction _toggleAction;

    public bool IsOpen { get; private set; }
    public event Action<bool> Toggled;

    private void Awake() => _toggleAction = InputSystem.actions.FindAction("Player/ToggleBag", true);

    private void OnEnable() => _toggleAction.performed += OnToggle;
    private void OnDisable() => _toggleAction.performed -= OnToggle;

    private void Start() => SetOpen(false);

    private void OnToggle(InputAction.CallbackContext ctx) => SetOpen(!IsOpen);

    private void SetOpen(bool open)
    {
        IsOpen = open;
        _panel.SetActive(open);

        Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = open;

        Toggled?.Invoke(open);
    }
}
