using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float interactionDistance = 3f;

    private void Update()
    {
        CheckInteraction();
    }

    private void CheckInteraction()
    {
        InputAction interactAction = new InputAction("Interact");
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactionDistance))
        {
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();
            if (interactable != null)
            {
                if (interactAction.WasPressedThisFrame())
                {
                    interactable.Interact();
                }
            }
        }
    }
}
