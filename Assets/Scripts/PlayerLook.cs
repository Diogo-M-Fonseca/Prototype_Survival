using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerLook : MonoBehaviour
{
    [SerializeField] private float _mouseSensitivity = 0.1f;
    [SerializeField] private Transform _cameraTransform;
    [SerializeField] private InventoryToggle _bagToggle;

    private float _xRotation = 0f;

    private void Update()
    {
        if (_bagToggle.IsOpen) return;
        if (Mouse.current == null) return;

        Vector2 delta = Mouse.current.delta.ReadValue() * _mouseSensitivity;

        _xRotation = Mathf.Clamp(_xRotation - delta.y, -90f, 90f);
        _cameraTransform.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * delta.x);
    }
}
