using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private InventoryToggle bagToggle;
    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private LayerMask interactionMask = ~0;

    private InputAction interactAction;

    private void Awake()
    {
        interactAction = InputSystem.actions.FindAction("Interact", true);
    }

    private void Update()
    {
        if (bagToggle != null && bagToggle.IsOpen) return;
        if (!interactAction.WasPressedThisFrame()) return;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance, interactionMask,
                            QueryTriggerInteraction.Ignore))
        {
            IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
            interactable?.Interact();
        }
    }
}
