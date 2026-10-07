using System;
using UnityEngine;

public class ItemTarget : MonoBehaviour, IItemTarget
{
    /// <summary>
    /// Se false, o alvo ignora todos os itens.
    /// </summary>
    [SerializeField] private bool _canUse = true;

    /// <summary>
    /// Regras deste alvo; vence a primeira cuja etiqueta o item tenha.
    /// </summary>
    [SerializeField] private ItemUseRule[] _rules;

    /// <summary>
    /// Ativa ou desativa o alvo.
    /// </summary>
    /// <param name="value">true para aceitar itens.</param>
    public void SetCanUse(bool value) => _canUse = value;

    /// <summary>
    /// Procura a primeira regra aplicável ao item.
    /// </summary>
    /// <param name="item">Item a testar.</param>
    /// <returns>A regra, ou null se nenhuma se aplicar.</returns>
    private ItemUseRule FindRule(Item item)
    {
        if (item == null || _rules == null) return null;

        foreach (ItemUseRule rule in _rules)
            if (rule != null && rule.Matches(item)) return rule;

        return null;
    }

    /// <summary>
    /// Aceita o item se existir uma regra para ele e calcula o resultado para o inventário.
    /// </summary>
    public bool CanReceive(Item item, GameObject user, out ItemUseOutcome outcome)
    {
        outcome = default;
        if (!_canUse) return false;

        ItemUseRule rule = FindRule(item);
        if (rule == null) return false;

        outcome = rule.ToOutcome();
        return true;
    }

    /// <summary>
    /// Mensagem do reticle para o item, se houver uma regra para ele.
    /// </summary>
    /// <param name="item"></param>
    /// <param name="user"></param>
    /// <returns></returns>
    public string GetUsePrompt(Item item, GameObject user)
    {
        return FindRule(item)?.UsePrompt;
    }


    /// <summary>
    /// Dispara o evento da regra aplicada.
    /// </summary>
    public void OnItemUsed(Item item, GameObject user)
    {
        FindRule(item)?.OnUsed?.Invoke(user);
    }
}
