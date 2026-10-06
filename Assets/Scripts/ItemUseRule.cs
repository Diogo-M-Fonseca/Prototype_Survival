using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class ItemUseRule
{
    /// <summary>
    /// Etiqueta que o item tem de ter para a regra se aplicar.
    /// </summary>
    [SerializeField] private ItemTag _requiredTag;

    /// <summary>
    /// Item deixado no lugar do usado.
    /// </summary>
    [SerializeField] private Item _result;

    /// <summary>
    /// Se false, o item não é gasto.
    /// </summary>
    [SerializeField] private bool _consumeItem = true;

    /// <summary>
    /// Evento disparado quando o item é usado; recebe quem o usou (ex.: abrir uma porta).
    /// </summary>
    [SerializeField] private UnityEvent<GameObject> _onUsed;

    /// <summary>
    /// Etiqueta exigida.
    /// </summary>
    public ItemTag RequiredTag => _requiredTag;

    /// <summary>
    /// Item deixado definido pela regra.
    /// </summary>
    public Item Result => _result;

    /// <summary>
    /// Se o item é gasto.
    /// </summary>
    public bool ConsumeItem => _consumeItem;

    /// <summary>
    /// Evento de uso.
    /// </summary>
    public UnityEvent<GameObject> OnUsed => _onUsed;

    /// <summary>
    /// Indica se esta regra se aplica ao item.
    /// </summary>
    /// <param name="item">Item a testar.</param>
    public bool Matches(Item item) => item != null && item.HasTag(_requiredTag);

    /// <summary>
    /// Converte a regra no resultado para o inventário: não gasta / gasta e deixa o item da regra / gasta e deixa o item normal.
    /// </summary>
    public ItemUseOutcome ToOutcome()
    {
        if (!_consumeItem) return ItemUseOutcome.Keep;
        if (_result != null) return ItemUseOutcome.ConsumeAndLeave(_result);
        return ItemUseOutcome.Consume;
    }
}
