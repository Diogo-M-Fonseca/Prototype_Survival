using UnityEngine;

/// <summary>
/// Mantém o crafting e o inventário (InventoryToggle) sempre sincronizados e fecha o menu com Esc.
/// A abertura é feita pelas CraftingStation (mesas). Pôr num GameObject ativo da cena (ex.: o Player).
/// A sincronização é por estado, em LateUpdate (sem eventos), por isso não depende da ordem em que
/// o inventário e o crafting reagem a quem os fechou.
/// </summary>
public class PlayerCrafting : MonoBehaviour
{
    [Tooltip("Menu de crafting. Se vazio, procura um na cena.")]
    [SerializeField] private CraftingMenu _crafting;

    [Tooltip("O InventoryToggle do jogador (o mesmo do PlayerLook). Se vazio, procura na cena.")]
    [SerializeField] private InventoryToggle _bagToggle;

    [Tooltip("Escreve na Console cada ação de sincronização (para diagnosticar descincronias).")]
    [SerializeField] private bool _logSync;

    private bool _lastCrafting;
    private bool _lastBag;
    private bool _bagOpenedByCrafting;

    private void Awake()
    {
#if UNITY_2023_1_OR_NEWER
        if (_crafting == null) _crafting = FindFirstObjectByType<CraftingMenu>();
        if (_bagToggle == null) _bagToggle = FindFirstObjectByType<InventoryToggle>();
#else
        if (_crafting == null) _crafting = FindObjectOfType<CraftingMenu>();
        if (_bagToggle == null) _bagToggle = FindObjectOfType<InventoryToggle>();
#endif

        if (_crafting == null)
            Debug.LogError("[PlayerCrafting] Não encontrei nenhum CraftingMenu na cena.", this);
        if (_bagToggle == null)
            Debug.LogError("[PlayerCrafting] Não encontrei nenhum InventoryToggle na cena.", this);

        // O inventário passa a ser o dono do cursor, para os dois não se contradizerem.
        if (_crafting != null && _bagToggle != null) _crafting.ManageCursor = false;
    }

    private void Update()
    {
        if (_crafting != null && _crafting.IsOpen && ClosePressed())
            _crafting.Close();
    }

    private void LateUpdate()
    {
        if (_crafting == null || _bagToggle == null) return;

        bool crafting = _crafting.IsOpen;
        bool bag = _bagToggle.IsOpen;

        if (!bag) _bagOpenedByCrafting = false;

        if (_logSync && (crafting != _lastCrafting || bag != _lastBag))
            Debug.Log($"[Sync] crafting {_lastCrafting}->{crafting} | bag {_lastBag}->{bag}", this);

        if (crafting != _lastCrafting)
        {
            // O crafting mudou: o inventário acompanha.
            if (crafting && !bag)
            {
                if (_logSync) Debug.Log("[Sync] crafting abriu -> bag.Open()", this);
                _bagToggle.Open();
                _bagOpenedByCrafting = true;
            }
            else if (!crafting && bag)
            {
                if (_logSync) Debug.Log("[Sync] crafting fechou -> bag.Close()", this);
                _bagToggle.Close();
            }
        }
        else if (bag != _lastBag && !bag && crafting)
        {
            // O jogador fechou o inventário: o crafting fecha também.
            if (_logSync) Debug.Log("[Sync] bag fechou -> crafting.Close()", this);
            _crafting.Close();
        }
        else if (!crafting && bag && _bagOpenedByCrafting)
        {
            // Rede de segurança: o crafting já fechou mas o inventário que ele abriu ficou aberto.
            if (_logSync) Debug.Log("[Sync] rede de segurança -> bag.Close()", this);
            _bagToggle.Close();
        }

        _lastCrafting = _crafting.IsOpen;
        _lastBag = _bagToggle.IsOpen;
    }

    private static bool ClosePressed()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = UnityEngine.InputSystem.Keyboard.current;
        return kb != null && kb.escapeKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }
}
