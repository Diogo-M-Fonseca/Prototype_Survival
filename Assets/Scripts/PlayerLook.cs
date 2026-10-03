using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerLook : MonoBehaviour
{
    [SerializeField] private float mouseSensitivity = 0.1f;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private InventoryToggle bagToggle;

    private float xRotation = 0f;

    private void Update()
    {
        if (bagToggle.IsOpen) return;
        if (Mouse.current == null) return;

        Vector2 delta = Mouse.current.delta.ReadValue() * mouseSensitivity;

        xRotation = Mathf.Clamp(xRotation - delta.y, -90f, 90f);
        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * delta.x);
    }
}
