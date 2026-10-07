using UnityEngine;

public readonly struct ItemUseOutcome
{
    /// <summary>
    /// True se uma unidade do item é gasta.
    /// </summary>
    public bool ConsumesItem { get; }

    /// <summary>
    /// True se o alvo define o item deixado, ignorando o LeftoverItem do próprio item.
    /// </summary>
    public bool OverridesLeftover { get; }

    /// <summary>
    /// Item deixado pelo alvo (só vale se OverridesLeftover for true; null = não deixa nada).
    /// </summary>
    public Item Leftover { get; }

    /// <summary>
    /// Construtor privado: usa as fábricas estáticas abaixo.
    /// </summary>
    private ItemUseOutcome(bool consumesItem, bool overridesLeftover, Item leftover)
    {
        ConsumesItem = consumesItem;
        OverridesLeftover = overridesLeftover;
        Leftover = leftover;
    }

    /// <summary>
    /// Gasta uma unidade e deixa o LeftoverItem normal do item (se tiver).
    /// </summary>
    public static ItemUseOutcome Consume => new ItemUseOutcome(true, false, null);

    /// <summary>
    /// Não gasta o item.
    /// </summary>
    public static ItemUseOutcome Keep => new ItemUseOutcome(false, false, null);

    /// <summary>
    /// Gasta uma unidade e deixa o item indicado pelo alvo.
    /// </summary>
    /// <param name="leftover">Item a deixar (null = não deixa nada).</param>
    public static ItemUseOutcome ConsumeAndLeave(Item leftover) => new ItemUseOutcome(true, true, leftover);
}
