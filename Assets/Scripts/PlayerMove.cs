using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMove : MonoBehaviour
{
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _gravity = -9.81f;
    private float _verticalVelocity;
    private CharacterController _controller;
    private InputAction _moveAction;

    void Start()
    {
        _controller = GetComponent<CharacterController>();
        _moveAction = InputSystem.actions.FindAction("Move");
        if (_moveAction == null)
        {
            Debug.LogError("Move action not found on PlayerMove script.");
        }

    }

    void Update()
    {
        if (CraftingMenu.AnyOpen) return;

        Vector2 moveInput = _moveAction.ReadValue<Vector2>();
        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        move = Vector3.ClampMagnitude(move, 1f);
        _controller.Move(move * _moveSpeed * Time.deltaTime);

        // Apply gravity
        if (_controller.isGrounded && _verticalVelocity < 0)
        {
            _verticalVelocity = -2f; 
        }
        else
        {
            _verticalVelocity += _gravity * Time.deltaTime;
        }

        _controller.Move(new Vector3(0, _verticalVelocity, 0) * Time.deltaTime);
    }
}
