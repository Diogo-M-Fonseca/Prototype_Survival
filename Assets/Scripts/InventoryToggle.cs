using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InventoryToggle : MonoBehaviour
{
    [SerializeField] private GameObject panel;

    private InputAction toggleAction;

    public bool IsOpen { get; private set; }
    public event Action<bool> Toggled;

    private void Awake() => toggleAction = InputSystem.actions.FindAction("Player/ToggleBag", true);

    private void OnEnable() => toggleAction.performed += OnToggle;
    private void OnDisable() => toggleAction.performed -= OnToggle;

    private void Start() => SetOpen(false);

    private void OnToggle(InputAction.CallbackContext ctx) => SetOpen(!IsOpen);

    private void SetOpen(bool open)
    {
        IsOpen = open;
        panel.SetActive(open);

        Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = open;

        Toggled?.Invoke(open);
    }
}
