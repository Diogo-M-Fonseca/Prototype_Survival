using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InventoryToggle : MonoBehaviour
{
    /// <summary>
    /// Painel da mochila que é ativado/desativado quando a mochila é aberta/fechada.
    /// </summary>
    [SerializeField] private GameObject _panel;

    /// <summary>
    /// Ação de input que alterna a mochila (deve ser configurada no Input System).
    /// </summary>
    private InputAction _toggleAction;

    /// <summary>
    /// True se a mochila está aberta, false caso contrário.
    /// </summary>
    public bool IsOpen { get; private set; }

    /// <summary>
    /// Disparado ao abrir (true) ou fechar (false).
    /// </summary>
    public event Action<bool> Toggled;

    /// <summary>
    /// Procura a ação de input (lança erro se não existir).
    /// </summary>
    private void Awake() => _toggleAction = InputSystem.actions.FindAction("Player/ToggleBag", true);

    /// <summary>
    /// Liga o callback da ação.
    /// </summary>
    private void OnEnable() => _toggleAction.performed += OnToggle;

    /// <summary>
    /// Desliga o callback da ação.
    /// </summary>
    private void OnDisable() => _toggleAction.performed -= OnToggle;

    /// <summary>
    /// Garante que o jogo começa com a mochila fechada.
    /// </summary>
    private void Start() => SetOpen(false);

    /// <summary>
    /// Inverte o estado ao premir a tecla.
    /// </summary>
    /// <param name="ctx"></param>
    private void OnToggle(InputAction.CallbackContext ctx)
    {
        // Se o menu de crafting acabou de fechar neste frame (ex.: com a mesma tecla)
        // e já fechou a mochila, ignora o input para não a reabrir.
        if (!IsOpen && CraftingMenu.BlockingInput) return;

        SetOpen(!IsOpen);
    }

    /// <summary>
    /// Abre a mochila.
    /// </summary>
    public void Open() => SetOpen(true);

    /// <summary>
    /// Fecha a mochila.
    /// </summary>
    public void Close() => SetOpen(false);

    /// <summary>
    /// Aplica o estado: painel, cursor e notificação.
    /// </summary>
    /// <param name="open"></param>
    private void SetOpen(bool open)
    {
        Debug.Log($"[Bag] SetOpen({open}) frame {Time.frameCount}\n{StackTraceUtility.ExtractStackTrace()}");
        // Atualiza o estado interno.
        IsOpen = open;

        // Ativa ou desativa o painel da mochila.
        _panel.SetActive(open);

        // Atualiza o cursor: visível e desbloqueado se a mochila estiver aberta, invisível e bloqueado caso contrário.
        Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = open;

        // Dispara o evento de notificação.
        Toggled?.Invoke(open);
    }
}