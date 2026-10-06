using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class HotbarController : MonoBehaviour
{
    /// <summary>
    /// Inventário de onde os itens são usados.
    /// </summary>
    [SerializeField] private Inventory _inventory;

    /// <summary>
    /// UI da hotbar, usada para destacar o slot selecionado e saber quantos slots existem.
    /// </summary>
    [SerializeField] private InventoryGridUI _hotbarUI;

    /// <summary>
    /// Toggle da mochila; com a mochila aberta a hotbar não responde.
    /// </summary>
    [SerializeField] private InventoryToggle _bagToggle;

    /// <summary>
    /// Interação do jogador; indica o que está sob a mira (usado para usar itens sobre o mundo).
    /// </summary>
    [SerializeField] private PlayerInteraction _interaction;

    /// <summary>
    /// Ação de input para selecionar um slot (teclas 1..N).
    /// </summary>
    private InputAction _selectAction;

    /// <summary>
    /// Ação de input para usar o item selecionado.
    /// </summary>
    private InputAction _useAction;

    /// <summary>
    /// Slot selecionado; -1 significa "nenhum".
    /// </summary>
    private int _selected = -1;

    /// <summary>
    /// Slot atualmente selecionado (-1 se nenhum).
    /// </summary>
    public int Selected => _selected;

    /// <summary>
    /// Disparado sempre que a seleção muda; o argumento é o novo índice
    /// </summary>
    public event Action<int> SelectionChanged;

    /// <summary>
    /// Indica se a mochila existe e está aberta.
    /// </summary>
    private bool BagOpen => _bagToggle != null && _bagToggle.IsOpen;

    private void Awake()
    {
        _selectAction = InputSystem.actions.FindAction("Player/HotbarSelect", true);
        _useAction = InputSystem.actions.FindAction("Player/Attack", true);
    }

    /// <summary>
    /// Liga os callbacks de input.
    /// </summary>
    private void OnEnable()
    {
        _selectAction.performed += OnSelect;
        _useAction.performed += OnUse;
    }

    /// <summary>
    /// Desliga os callbacks de input.
    /// </summary>
    private void OnDisable()
    {
        _selectAction.performed -= OnSelect;
        _useAction.performed -= OnUse;
    }

    private void Start() => Select(-1);

    private void Update()
    {
        // Ignora o scroll com a mochila aberta ou sem rato/slots
        if (BagOpen) return;
        if (Mouse.current == null || _hotbarUI.SlotCount == 0) return;

        // Sem movimento da roda não há nada a fazer
        float scroll = Mouse.current.scroll.ReadValue().y;
        if (Mathf.Approximately(scroll, 0f)) return;

        // Se nada estava selecionado, começa no primeiro (scroll para cima) ou no último (para baixo);
        // caso contrário avança/recua com wrap-around
        int direction = scroll > 0f ? 1 : -1;
        int next = _selected < 0
            ? (direction > 0 ? 0 : _hotbarUI.SlotCount - 1)
            : (_selected + direction + _hotbarUI.SlotCount) % _hotbarUI.SlotCount;

        Select(next);
    }

    /// <summary>
    /// Seleção por tecla: o índice vem da posição do binding premido.
    /// </summary>
    private void OnSelect(InputAction.CallbackContext ctx)
    {
        int index = ctx.action.GetBindingIndexForControl(ctx.control);
        if (index < 0 || index >= _hotbarUI.SlotCount) return;

        // Premir a tecla do slot já selecionado desseleciona-o
        Select(_selected == index ? -1 : index);
    }

    /// <summary>
    /// Usa o item do slot selecionado: primeiro sobre o que está sob a mira (ex.: chave numa porta), se isso não tiver efeito, normalmente (ex.: beber).
    /// </summary>
    private void OnUse(InputAction.CallbackContext ctx)
    {
        if (BagOpen || _selected < 0) return;

        // Objeto que o jogador está a olhar (null se nada)
        Collider looked = _interaction != null ? _interaction.LookedCollider : null;
        if (looked != null && _inventory.UseSlotOn(_selected, gameObject, looked.gameObject)) return;

        // Sem alvo ou o item não faz nada com ele: uso normal
        _inventory.UseSlot(_selected, gameObject);
    }

    /// <summary>
    /// Define o slot selecionado, atualiza o destaque na UI e avisa os ouvintes.
    /// </summary>
    /// <param name="index">Novo slot (-1 para nenhum).</param>
    private void Select(int index)
    {
        _selected = index;
        _hotbarUI.SetSelected(_selected);
        SelectionChanged?.Invoke(_selected);
    }
}
