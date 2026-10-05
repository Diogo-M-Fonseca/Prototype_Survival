using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private Camera _playerCamera;
    [SerializeField] private InventoryToggle _bagToggle;
    [SerializeField] private float _interactionDistance = 3f;
    [SerializeField] private LayerMask _interactionMask = ~0;
    [SerializeField] private string _bindingGroup = "Keyboard&Mouse";

    private readonly RaycastHit[] _hits = new RaycastHit[16];
    private InputAction _interactAction;

    public IInteractable Current { get; private set; }

    public string InteractKey
    {
        get
        {
            string key = _interactAction.GetBindingDisplayString(
                InputBinding.DisplayStringOptions.DontIncludeInteractions,
                string.IsNullOrEmpty(_bindingGroup) ? null : _bindingGroup);
            return string.IsNullOrEmpty(key) ? "E" : key;

        }
    }

    private void Awake()
    {
        _interactAction = InputSystem.actions.FindAction("Interact", true);
    }

    private void Update()
    {
        bool bagOpen = _bagToggle != null && _bagToggle.IsOpen;

        Current = bagOpen ? null : FindTarget();

        if (Current != null && _interactAction.WasPressedThisFrame())
        {
            Current.Interact(gameObject);
        }
    }

    private IInteractable FindTarget()
    {
        Ray ray = _playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        int count = Physics.RaycastNonAlloc(ray, _hits, 
            _interactionDistance, _interactionMask,
            QueryTriggerInteraction.Ignore);

        Collider closest = null;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            Collider c = _hits[i].collider;
            if (c.transform.IsChildOf(transform)) continue;

            if (_hits[i].distance < closestDistance)
            {
                closestDistance = _hits[i].distance;
                closest = c;
            }
        }

        if (closest == null) return null;

        IInteractable target = closest.GetComponentInParent<IInteractable>();
        return target != null && target.CanInteract ? target : null;
    }
}
