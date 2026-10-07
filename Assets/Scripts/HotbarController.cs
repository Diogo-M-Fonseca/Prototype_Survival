using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class HotbarController : MonoBehaviour
{
    /// <summary>
    /// Invent�rio de onde os itens s�o usados.
    /// </summary>
    [SerializeField] private Inventory _inventory;

    /// <summary>
    /// UI da hotbar, usada para destacar o slot selecionado e saber quantos slots existem.
    /// </summary>
    [SerializeField] private InventoryGridUI _hotbarUI;

    /// <summary>
    /// Toggle da mochila; com a mochila aberta a hotbar n�o responde.
    /// </summary>
    [SerializeField] private InventoryToggle _bagToggle;

    /// <summary>
    /// Intera��o do jogador; indica o que est� sob a mira (usado para usar itens sobre o mundo).
    /// </summary>
    [SerializeField] private PlayerInteraction _interaction;

    /// <summary>
    /// Grupo de bindings usado para mostrar o nome da tecla em uso
    /// </summary>
    [SerializeField] private string _bindingGroup = "Keyboard&Mouse";

    /// <summary>
    /// A��o de input para selecionar um slot (teclas 1..N).
    /// </summary>
    private InputAction _selectAction;

    /// <summary>
    /// A��o de input para usar o item selecionado.
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
    /// Disparado sempre que a sele��o muda; o argumento � o novo �ndice
    /// </summary>
    public event Action<int> SelectionChanged;

    /// <summary>
    /// Indica se a mochila existe e est� aberta.
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

        // Sem movimento da roda n�o h� nada a fazer
        float scroll = Mouse.current.scroll.ReadValue().y;
        if (Mathf.Approximately(scroll, 0f)) return;

        // Se nada estava selecionado, come�a no primeiro (scroll para cima) ou no �ltimo (para baixo);
        // caso contr�rio avan�a/recua com wrap-around
        int direction = scroll > 0f ? 1 : -1;
        int next = _selected < 0
            ? (direction > 0 ? 0 : _hotbarUI.SlotCount - 1)
            : (_selected + direction + _hotbarUI.SlotCount) % _hotbarUI.SlotCount;

        Select(next);
    }

    /// <summary>
    /// Sele��o por tecla: o �ndice vem da posi��o do binding premido.
    /// </summary>
    private void OnSelect(InputAction.CallbackContext ctx)
    {
        int index = ctx.action.GetBindingIndexForControl(ctx.control);
        if (index < 0 || index >= _hotbarUI.SlotCount) return;

        // Premir a tecla do slot j� selecionado desseleciona-o
        Select(_selected == index ? -1 : index);
    }

    /// <summary>
    /// Usa o item do slot selecionado: primeiro sobre o que est� sob a mira (ex.: chave numa porta), se isso n�o tiver efeito, normalmente (ex.: beber).
    /// </summary>
    private void OnUse(InputAction.CallbackContext ctx)
    {
        if (BagOpen || _selected < 0) return;

        // Objeto que o jogador est� a olhar (null se nada)
        Collider looked = _interaction != null ? _interaction.LookedCollider : null;
        if (looked != null && TryAttackWithCrowbar(looked)) return;

        if (looked != null && _inventory.UseSlotOn(_selected, gameObject, looked.gameObject)) return;

        // Sem alvo ou o item n�o faz nada com ele: uso normal
        _inventory.UseSlot(_selected, gameObject);
    }

    private bool TryAttackWithCrowbar(Collider targetCollider)
    {
        ItemStack stack = _inventory[_selected];
        if (stack.IsEmpty || !(stack.Item is Tools tool)) return false;
        if (tool.ToolType != ToolTypes.Crowbar || !tool.CanAttack) return false;

        IDestroyable target = targetCollider.GetComponentInParent<IDestroyable>();
        if (target == null) return false;

        target.TakeDamage(tool.AttackPower);
        return true;
    }

    /// <summary>
    /// Indica se o item selecionado tem efeito sobre o que est� sob a mira
    /// </summary>
    /// <param name="key"></param>
    /// <param name="prompt"></param>
    /// <returns>Tecla e a mensagem para mostrar no ret�culo.</returns>
    public bool TryGetUsePrompt(out string key, out string prompt)
    {
        key = null;
        prompt = null;

        if (BagOpen || _selected < 0 || _interaction == null) return false;

        ItemStack stack = _inventory[_selected];
        Collider looked = _interaction.LookedCollider;
        if (stack.IsEmpty || looked == null) return false;

        IItemTarget target = looked.GetComponentInParent<IItemTarget>();
        if (target == null || !target.CanReceive(stack.Item, gameObject, out _)) return false;

        prompt = target.GetUsePrompt(stack.Item, gameObject);
        if (string.IsNullOrEmpty(prompt)) prompt = "use " + stack.Item.DisplayName;

        key = _useAction.GetBindingDisplayString(
            InputBinding.DisplayStringOptions.DontIncludeInteractions,
            string.IsNullOrEmpty(_bindingGroup) ? null : _bindingGroup);
        if (string.IsNullOrEmpty(key)) key = "Click";

        return true;
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
