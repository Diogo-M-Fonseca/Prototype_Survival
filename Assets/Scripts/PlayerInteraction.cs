using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    /// <summary>
    /// Câmara do jogador (origem do raycast).
    /// </summary>
    [SerializeField] private Camera _playerCamera;

    /// <summary>
    /// Toggle da mochila; com ela aberta não se interage com o mundo.
    /// </summary>
    [SerializeField] private InventoryToggle _bagToggle;

    /// <summary>
    /// Distância máxima de interação, em metros.
    /// </summary>
    [SerializeField] private float _interactionDistance = 3f;

    /// <summary>
    /// Layers consideradas pelo raycast (por omissão, todas).
    /// </summary>
    [SerializeField] private LayerMask _interactionMask = ~0;

    /// <summary>
    /// Grupo de bindings usado para mostrar o nome da tecla.
    /// </summary>
    [SerializeField] private string _bindingGroup = "Keyboard&Mouse";

    /// <summary>
    /// Buffer reutilizado pelo raycast para não gerar lixo (lista de acertos).
    /// </summary>
    private readonly RaycastHit[] _hits = new RaycastHit[16];

    /// <summary>
    /// Ação de input de interação.
    /// </summary>
    private InputAction _interactAction;

    /// <summary>
    /// Alvo interagível atual (null se não houver).
    /// </summary>
    public IInteractable Current { get; private set; }

    /// <summary>
    /// Colisor mais próximo sob a mira (interagível ou não); null se nada ou com a mochila aberta.
    /// </summary>
    public Collider LookedCollider { get; private set; }

    /// <summary>
    /// Nome legível da tecla de interação (usa "E" se não for possível obter).
    /// </summary>
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
        // Com a mochila aberta não há alvo
        bool bagOpen = _bagToggle != null && _bagToggle.IsOpen;

        Current = bagOpen ? null : FindTarget();
        if (bagOpen) LookedCollider = null;

        // Interage no frame em que a tecla é premida
        if (Current != null && _interactAction.WasPressedThisFrame())
        {
            Current.Interact(gameObject);
        }
    }

    /// <summary>
    /// Lança um raio do centro do ecrã e devolve o interagível mais próximo (ou null).
    /// </summary>
    private IInteractable FindTarget()
    {
        LookedCollider = null;

        // Raio a partir do centro da câmara; RaycastNonAlloc preenche _hits sem alocar memória
        Ray ray = _playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        int count = Physics.RaycastNonAlloc(ray, _hits,
            _interactionDistance, _interactionMask,
            QueryTriggerInteraction.Ignore);

        // Procura o colisor mais próximo, ignorando os do próprio jogador
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

        // Guarda o que está sob a mira, mesmo que não seja interagível
        LookedCollider = closest;
        if (closest == null) return null;

        // O que está mais perto tem de ser interagível (e permitir interação), senão não há alvo
        IInteractable target = closest.GetComponentInParent<IInteractable>();
        return target != null && target.CanInteract ? target : null;
    }
}
