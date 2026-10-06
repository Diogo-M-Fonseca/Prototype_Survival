using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class InventoryGridUI : MonoBehaviour
{
    /// <summary>
    /// Inventário mostrado por esta grelha (pode ser trocado com bind).
    /// </summary>
    [SerializeField] private Inventory _inventory;

    /// <summary>
    /// prefab de um slot.
    /// </summary>
    [SerializeField] private SlotUI _slotPrefab;

    /// <summary>
    /// Transform pai onde os slots são instanciados (normalmente um GridLayoutGroup).
    /// </summary>
    [SerializeField] private Transform _Grid;

    /// <summary>
    /// Numero maximo de slots a mostrar.
    /// </summary>
    [SerializeField, Min(1)] private int _slotCount = 9;

    /// <summary>
    /// Lista de slots já criados.
    /// </summary>
    private readonly List<SlotUI> _slots = new List<SlotUI>();

    /// <summary>
    /// Numero de slots atualmente ativos.
    /// </summary>
    private int _count;

    /// <summary>
    /// Inventário atualmente ligado.
    /// </summary>
    public Inventory Inventory => _inventory;

    /// <summary>
    /// Getter do numero de slots atualmente ativos.
    /// </summary>
    public int SlotCount => _count;

    /// <summary>
    /// Inicio de um drag: grelha, indice do slot, dados do ponteiro.
    /// </summary>
    public event Action<InventoryGridUI, int, PointerEventData> BeginDrag;

    /// <summary>
    /// Drag em andamento: dados do ponteiro.
    /// </summary>
    public event Action<PointerEventData> Drag;

    /// <summary>
    /// Fim do drag: sem parametros.
    /// </summary>
    public event Action EndDrag;

    /// <summary>
    /// Drop sobre o slot: grelha, indice do slot.
    /// </summary>
    public event Action<InventoryGridUI, int> Drop;

    // Se já houver inventário atribuido no insppector, liga-se a ele.
    private void Awake()
    {
        if (_inventory != null) Bind(_inventory);
    }

    // Ao ativar, redesenha os slots.
    private void OnEnable() => Refresh();

    // Ao desativar, deixa de ouvir o inventário.
    private void OnDestroy()
    {
        if (_inventory != null) _inventory.Changed -= Refresh;
    }

    /// <summary>
    /// Liga a grelha a um inventário: cria/reutiliza slots, esconde os que sobram e passam a ouvir as mudanças.
    /// </summary>
    /// <param name="inventory"></param>
    public void Bind(Inventory inventory)
    {
        // Deixa de ouvir o inventário anterior, se houver.
        if (_inventory != null) _inventory.Changed -= Refresh;

        // Atribui o novo inventário e calcula o numero de slots a mostrar.
        _inventory = inventory;
        _count = _inventory != null ? Mathf.Min(_slotCount, _inventory.Size) : 0;

        // Prepara os slots necessários: cria novos só se ainda não existirem.
        for (int i = 0; i < _count; i++)
        {
            // Se não houver slot suficiente, instancia um novo.
            if (i >= _slots.Count)
                _slots.Add(Instantiate(_slotPrefab, _Grid));

            // Inicializa o slot: ativa, atribui o dono e o indice, desmarca e deseleciona.
            SlotUI slot = _slots[i];
            slot.gameObject.SetActive(true);
            slot.Init(this, i);
            slot.SetSelected(false);
            slot.SetMarked(false);
        }

        // Desativa os slots que sobram.
        for (int i = _count; i < _slots.Count; i++)
            _slots[i].gameObject.SetActive(false);

        // Se não houver inventário, não há nada a fazer.
        if (_inventory == null) return;

        // Começa a ouvir as mudanças do inventário e redesenha os slots.
        _inventory.Changed += Refresh;
        Refresh();
    }

    /// <summary>
    /// Desliga a grelha do inventário atual.
    /// </summary>
    public void Unbind()
    {
        // Deixa de ouvir o inventário atual, se houver.
        Bind(null);
    }

    /// <summary>
    /// Atualiza o conteudo visual de todos os slots.
    /// </summary>
    public void Refresh()
    {
        // Se não houver slots, não há nada a fazer.
        if (_slots == null) return;

        // Atualiza cada slot com o conteudo do inventário.
        for (int i = 0; i < _count; i++)
            _slots[i].Set(_inventory[i]);
    }

    /// <summary>
    /// Destaca um slot como slecionado.
    /// </summary>
    /// <param name="index"></param>
    public void SetSelected(int index)
    {
        // Atualiza cada slot para mostrar se está selecionado ou não.
        for (int i = 0; i < _count; i++)
            _slots[i].SetSelected(i == index);
    }

    /// <summary>
    /// Marca um slot com outra cor
    /// </summary>
    /// <param name="index"></param>
    public void SetMarked(int index)
    {
        // Atualiza cada slot para mostrar se está marcado ou não.
        for (int i = 0; i < _count; i++)
            _slots[i].SetMarked(i == index);
    }

    /// <summary>
    /// Chamado por um SlotUI quando começa um drag.
    /// </summary>
    /// <param name="index"></param>
    /// <param name="e"></param>
    public void NotifyBeginDrag(int index, PointerEventData e) => BeginDrag?.Invoke(this, index, e);

    /// <summary>
    /// Chamado por um SlotUI quando o drag está em andamento.
    /// </summary>
    /// <param name="e"></param>
    public void NotifyDrag(PointerEventData e) => Drag?.Invoke(e);

    /// <summary>
    /// Chamado por um SlotUI quando o drag termina.
    /// </summary>
    public void NotifyEndDrag() => EndDrag?.Invoke();

    /// <summary>
    /// Chamado por um SlotUI quando um item é dropado sobre ele.
    /// </summary>
    /// <param name="index"></param>
    public void NotifyDrop(int index) => Drop?.Invoke(this, index);
}
