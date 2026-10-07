using UnityEngine;

public interface IItemTarget
{
    /// <summary>
    /// Pergunta se o item tem efeito neste alvo. Não pode ter efeitos secundários: só diz se aceita e qual o resultado para o inventário.
    /// </summary>
    /// <param name="item">Item que o jogador tem na mão.</param>
    /// <param name="user">Quem usa o item.</param>
    /// <param name="outcome">Se o item é gasto e o que fica no lugar.</param>
    /// <returns>true se o alvo aceita o item.</returns>
    bool CanReceive(Item item, GameObject user, out ItemUseOutcome outcome);

    /// <summary>
    /// Mensagem a mostrar no retículo quando o jogador aponta para o alvo com este item. Só faz sentido chamar depois de CanReceive devolver true.
    /// </summary>
    /// <param name="item">Item que o jogador tem na mão.</param>
    /// <param name="user">Quem usa o item.</param>
    string GetUsePrompt(Item item, GameObject user);

    /// <summary>
    /// Chamado depois de o inventário aplicar o resultado: aqui o alvo reage.
    /// </summary>
    /// <param name="item">Item usado.</param>
    /// <param name="user">Quem usou o item.</param>
    void OnItemUsed(Item item, GameObject user);
}
