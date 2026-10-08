using System;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class Inventory : MonoBehaviour, IInventory
{
    /// <summary>
    /// Número total de slots do inventário.
    /// </summary>
    [SerializeField, Min(1)] private int _size = 20;

    /// <summary>
    /// Velocidade do desgaste: unidades de durabilidade perdidas por segundo (0 desativa).
    /// </summary>
    [SerializeField, Min(0f)] private float _durabilityRate = 1f;

    /// <summary>
    /// Intervalo m�nimo (s) entre notifica��es Changed causadas apenas pelo desgaste. Um item que parte notifica logo, sem esperar.
    /// </summary>
    [SerializeField, Min(0.05f)] private float _durabilityNotifyInterval = 0.25f;

    /// <summary>
    /// Slots do inventário.
    /// </summary>
    private ItemStack[] _slots;

    /// <summary>
    /// Tempo acumulado desde a última notificação por desgaste.
    /// </summary>
    private float _notifyTimer;

    /// <summary>
    /// true se houve desgaste que ainda não foi notificado à UI.
    /// </summary>
    private bool _durabilityDirty;

    /// <summary>
    /// Disparado sempre que o conteúdo do inventário muda.
    /// </summary>
    public event Action Changed;

    /// <summary>
    /// N�mero de slots.
    /// </summary>
    public int Size => _size;

    /// <summary>
    /// Taxa de desgaste atual.
    /// </summary>
    public float DurabilityRate => _durabilityRate;

    /// <summary>
    /// Acesso s� de leitura ao stack de um slot.
    /// </summary>
    /// <param name="i">�ndice do slot.</param>
    public ItemStack this[int i] => _slots[i];

    /// <summary>
    /// Cria o array de slots (todos vazios).
    /// </summary>
    private void Awake() => _slots = new ItemStack[_size];

    /// <summary>
    /// Aplica o desgaste de durabilidade a todos os itens que a têm.
    /// Itens que chegam a 0 são removidos.
    /// </summary>
    private void Update()
    {
        // Desgaste desativado
        if (_durabilityRate <= 0f) return;

        // Quanto desgaste cabe neste frame
        float decay = Time.deltaTime * _durabilityRate;
        bool broke = false;

        // Aplica o desgaste a cada slot.
        for (int i = 0; i < _slots.Length; i++)
        {
            // S� interessam slots com item que tenha durabilidade
            ItemStack slot = _slots[i];
            if (slot.IsEmpty || !slot.Item.HasDurability) continue;

            // Calcula a durabilidade restante após o desgaste.
            float remaining = slot.DurabilityRemaining - decay;

            if (remaining <= 0f)
            {
                // Item partiu: o slot fica vazio
                _slots[i] = default;
                broke = true;
            }
            else
            {
                // Atualiza o slot com a nova durabilidade.
                _slots[i] = slot.WithDurability(remaining);
            }
            _durabilityDirty = true;
        }

        // Se não houve mudanças, não precisa notificar.
        if (!_durabilityDirty) return;

        // O desgaste aplica-se todos os frames, mas a UI só é avisada de vez em quando
        // (ou imediatamente se um item partiu).
        _notifyTimer += Time.deltaTime;
        if (broke || _notifyTimer >= _durabilityNotifyInterval)
        {
            _notifyTimer = 0f;
            _durabilityDirty = false;
            Changed?.Invoke();
        }
    }

    /// <summary>
    /// Adiciona itens: primeiro completa stacks existentes do mesmo item, depois usa slots vazios.
    /// </summary>
    /// <param name="item">Item a adicionar.</param>
    /// <param name="amount">Quantidade a adicionar.</param>
    /// <returns>Quantidade que N�O coube (0 = tudo adicionado).</returns>
    public int Add(Item item, int amount)
    {
        // Se o item for nulo ou a quantidade for menor ou igual a zero, n�o faz nada e retorna a quantidade original.
        if (item == null || amount <= 0) return amount;

        // Quantidade restante de itens que ainda precisam ser adicionados.
        int remaining = amount;

        // 1.� passagem: preencher stacks existentes do mesmo item
        for (int i = 0; i < _slots.Length && remaining > 0; i++)
        {
            if (_slots[i].IsEmpty || _slots[i].Item != item) continue;

            int add = Mathf.Min(remaining, item.MaxStack - _slots[i].Amount);
            if (add <= 0) continue;

            _slots[i] = _slots[i].WithAmount(_slots[i].Amount + add);
            remaining -= add;
        }

        // 2.� passagem: criar novos stacks nos slots vazios
        for (int i = 0; i < _slots.Length && remaining > 0; i++)
        {
            if (!_slots[i].IsEmpty) continue;

            int add = Mathf.Min(remaining, item.MaxStack);
            _slots[i] = new ItemStack(item, add);
            remaining -= add;
        }

        // Só avisa se algo foi realmente adicionado
        if (remaining != amount) Changed?.Invoke();
        // Retorna a quantidade de itens que não couberam no inventário.
        return remaining;
    }

    /// <summary>
    /// Verifica se cabe a quantidade pedida sem a adicionar.
    /// </summary>
    /// <param name="item">Item a testar.</param>
    /// <param name="amount">Quantidade a testar.</param>
    /// <returns>true se couber tudo.</returns>
    public bool CanAdd(Item item, int amount)
    {
        // Se o item for nulo, n�o cabe.
        if (item == null) return false;

        // Vai descontando o espa�o dispon�vel � quantidade pedida
        foreach (ItemStack s in _slots)
        {
            if (amount <= 0) break;

            if (s.IsEmpty) amount -= item.MaxStack;                         // slot vazio: cabe um stack completo
            else if (s.Item == item) amount -= item.MaxStack - s.Amount;    // mesmo item: cabe o que falta para encher
        }
        return amount <= 0;
    }

    /// <summary>
    /// Conta quantas unidades de um item existem no invent�rio (somando todos os stacks).
    /// </summary>
    /// <param name="item">Item a contar.</param>
    public int Count(Item item)
    {
        // Se o item for nulo, não há nada para contar.
        if (item == null) return 0;

        // Soma a quantidade de cada slot que contém o item.
        int total = 0;
        foreach (ItemStack s in _slots)
            if (!s.IsEmpty && s.Item == item) total += s.Amount;
        return total;
    }

    /// <summary>
    /// Indica se existe pelo menos a quantidade pedida do item.
    /// </summary>
    /// <param name="item"></param>
    /// <param name="amount"></param>
    /// <returns></returns>
    public bool Has(Item item, int amount) => Count(item) >= amount;

    /// <summary>Remove uma quantidade de um item, come�ando pelos �ltimos slots.</summary>
    /// <param name="item">Item a remover.</param>
    /// <param name="amount">Quantidade a remover.</param>
    /// <returns>true se removeu; false se n�o havia quantidade suficiente.</returns>
    public bool Remove(Item item, int amount)
    {
        // Se o item for nulo, a quantidade for menor ou igual a zero, ou n�o houver quantidade suficiente, n�o faz nada e retorna false.
        if (item == null || amount <= 0 || !Has(item, amount)) return false;

        // Percorre de tr�s para a frente, retirando de cada stack at� cumprir a quantidade
        for (int i = _slots.Length - 1; i >= 0 && amount > 0; i--)
        {
            // Se o slot estiver vazio ou n�o for o item procurado, pula para o pr�ximo.
            if (_slots[i].IsEmpty || _slots[i].Item != item) continue;

            // Calcula quanto pode retirar deste slot, que � o m�nimo entre a quantidade restante e a quantidade no slot.
            int take = Mathf.Min(amount, _slots[i].Amount);
            int left = _slots[i].Amount - take;
            _slots[i] = left <= 0 ? default : _slots[i].WithAmount(left);
            amount -= take;
        }

        // Dispara o evento Changed para notificar que o invent�rio mudou.
        Changed?.Invoke();
        // Retorna true para indicar que a remo��o foi bem-sucedida.
        return true;
    }

    /// <summary>Usa o item de um slot; se o uso tiver sucesso, consome uma unidade (e pode deixar outro item).</summary>
    /// <param name="index">Slot a usar.</param>
    /// <param name="user">Quem usa o item (ex.: o jogador).</param>
    /// <returns>true se o item foi usado.</returns>
    public bool UseSlot(int index, GameObject user)
    {
        // O cast para uint apanha de uma vez índices negativos e acima do limite
        if ((uint)index >= (uint)_slots.Length) return false;

        ItemStack slot = _slots[index];
        if (slot.IsEmpty) return false;

        // Verifica ANTES de usar se o item deixado cabe, para n�o gastar o efeito sem lugar para ele
        Item leftover = slot.Item.LeftoverItem;
        if (!HasRoomForLeftover(slot, leftover)) return false;

        if (!slot.Item.Use(user)) return false;
        slot.Item.PlayUseSound(user);

        ApplyConsumption(index, slot, leftover);
        return true;
    }

    /// <summary>
    /// Usa o item de um slot sobre um objeto do mundo que implemente IItemTarget
    /// (fonte, porta, �rvore...). O alvo decide se aceita e o que acontece ao item.
    /// </summary>
    /// <param name="index">Slot a usar.</param>
    /// <param name="user">Quem usa o item.</param>
    /// <param name="target">Objeto do mundo (procura-se um IItemTarget nele ou nos pais).</param>
    /// <returns>true se o alvo aceitou o item.</returns>
    public bool UseSlotOn(int index, GameObject user, GameObject target)
    {
        if (target == null || (uint)index >= (uint)_slots.Length) return false;

        ItemStack slot = _slots[index];
        if (slot.IsEmpty) return false;

        // O alvo decide se aceita e qual o resultado (sem ainda ter efeitos)
        IItemTarget receiver = target.GetComponentInParent<IItemTarget>();
        if (receiver == null || !receiver.CanReceive(slot.Item, user, out ItemUseOutcome outcome)) return false;

        Item used = slot.Item;

        if (outcome.ConsumesItem)
        {
            // Item deixado: o definido pelo alvo ou, se n�o houver, o do pr�prio item
            Item leftover = outcome.OverridesLeftover ? outcome.Leftover : used.LeftoverItem;
            if (!HasRoomForLeftover(slot, leftover)) return false;

            ApplyConsumption(index, slot, leftover);
        }

        // S� agora o alvo reage (o invent�rio j� est� atualizado)
        receiver.OnItemUsed(used, user);
        return true;
    }

    /// <summary>
    /// Verifica se o item deixado cabe. Se a unidade usada era a �ltima, ocupa o mesmo slot (cabe sempre);
    /// caso contr�rio tem de caber noutro slot.
    /// </summary>
    /// <param name="slot">Stack que vai ser gasto.</param>
    /// <param name="leftover">Item a deixar (pode ser null).</param>
    private bool HasRoomForLeftover(ItemStack slot, Item leftover)
        => leftover == null || slot.Amount <= 1 || CanAdd(leftover, 1);

    /// <summary>Gasta uma unidade do slot e coloca o item deixado (no mesmo slot se era a �ltima, ou noutro).</summary>
    /// <param name="index">Slot gasto.</param>
    /// <param name="slot">C�pia do stack antes de gastar.</param>
    /// <param name="leftover">Item a deixar (pode ser null).</param>
    private void ApplyConsumption(int index, ItemStack slot, Item leftover)
    {
        int left = slot.Amount - 1;

        if (left <= 0)
        {
            // Era a �ltima unidade: o slot passa a ter o item deixado (ou fica vazio)
            _slots[index] = leftover != null ? new ItemStack(leftover, 1) : default;
        }
        else
        {
            // Restam unidades: desconta uma e junta o item deixado ao invent�rio
            _slots[index] = slot.WithAmount(left);
            if (leftover != null) Add(leftover, 1);
        }

        Changed?.Invoke();
    }

    /// <summary>Move o stack inteiro de um slot para outro invent�rio (se couber).</summary>
    /// <param name="index">Slot de origem neste invent�rio.</param>
    /// <param name="target">Invent�rio de destino.</param>
    /// <returns>true se transferiu.</returns>
    public bool TransferSlot(int index, Inventory target)
    {
        // Se o invent�rio alvo for nulo, ou for o mesmo invent�rio, ou o index for inv�lido, retorna false.
        if (target == null || target == this) return false;
        if ((uint)index >= (uint)_slots.Length) return false;

        // Pega o item do slot. Se estiver vazio, retorna false.
        ItemStack s = _slots[index];
        if (s.IsEmpty) return false;

        // S� transfere se o destino tiver espa�o
        if (!target.CanAdd(s.Item, s.Amount)) return false;
        if (!target.TryAddStack(s)) return false;

        // S� limpa a origem depois de o destino ter aceitado
        _slots[index] = default;
        Changed?.Invoke();
        return true;
    }

    /// <summary>
    /// Adiciona um stack inteiro. Itens com durabilidade s�o copiados tal como est�o
    /// para um slot vazio (para n�o perder a durabilidade); os outros usam Add normal.
    /// </summary>
    /// <param name="stack">Stack a adicionar.</param>
    private bool TryAddStack(ItemStack stack)
    {
        // Se o stack estiver vazio, n�o faz nada e retorna false.
        if (stack.IsEmpty) return false;

        // Sem durabilidade: Add resolve (empilha e distribui)
        if (!stack.Item.HasDurability)
            return Add(stack.Item, stack.Amount) == 0;

        // Com durabilidade: coloca o stack no primeiro slot vazio, mantendo o estado
        for (int i = 0; i < _slots.Length; i++)
        {
            // Se o slot n�o estiver vazio, pula para o pr�ximo.
            if (!_slots[i].IsEmpty) continue;

            // Coloca o stack inteiro no slot vazio.
            _slots[i] = stack;
            // Dispara o evento Changed para notificar que o invent�rio mudou.
            Changed?.Invoke();
            // Retorna true para indicar que o stack foi adicionado com sucesso.
            return true;
        }

        // Se n�o encontrou nenhum slot vazio, retorna false.
        return false;
    }

    /// <summary>
    /// Move um stack para um slot (do mesmo ou de outro invent�rio). Se o destino tiver o mesmo item,
    /// junta o que couber; caso contr�rio troca os dois slots.
    /// </summary>
    /// <param name="from">Slot de origem neste invent�rio.</param>
    /// <param name="target">Invent�rio de destino (pode ser este).</param>
    /// <param name="to">Slot de destino.</param>
    /// <returns>true se algo mudou.</returns>
    public bool MoveOrSwap(int from, Inventory target, int to)
    {
        // Se o invent�rio alvo for nulo, ou os �ndices forem inv�lidos, ou se for o mesmo slot, retorna false.
        if (target == null) return false;
        if ((uint)from >= (uint)_slots.Length || (uint)to >= (uint)target._slots.Length) return false;
        if (target == this && from == to) return false;

        // Pega os stacks dos slots de origem e destino.
        ItemStack a = _slots[from];
        ItemStack b = target._slots[to];
        // Se o slot de origem estiver vazio, n�o h� nada para mover, retorna false.
        if (a.IsEmpty) return false;

        // Mesmo item e ainda h� espa�o no destino: junta
        if (!b.IsEmpty && b.Item == a.Item)
        {
            // Calcula quanto espa�o ainda h� no stack de destino.
            int space = a.Item.MaxStack - b.Amount;

            // Se houver espa�o, move o que couber.
            if (space > 0)
            {
                // Calcula quanto pode mover, que � o m�nimo entre o espa�o dispon�vel e a quantidade no slot de origem.
                int move = Mathf.Min(space, a.Amount);
                target._slots[to] = b.WithAmount(b.Amount + move);

                // O que sobrar fica na origem
                int left = a.Amount - move;
                _slots[from] = left <= 0 ? default : a.WithAmount(left);

                // Dispara o evento Changed para notificar que o invent�rio mudou.
                Changed?.Invoke();

                // Se o invent�rio alvo for diferente deste, tamb�m dispara o evento Changed no invent�rio alvo.
                if (target != this) target.Changed?.Invoke();

                // Retorna true para indicar que a opera��o foi bem-sucedida.
                return true;
            }
        }

        // Caso contr�rio: troca os dois slots (se o destino estiver vazio, � um simples movimento)
        _slots[from] = b;
        target._slots[to] = a;

        // Dispara o evento Changed para notificar que o invent�rio mudou.
        Changed?.Invoke();
        // Se o invent�rio alvo for diferente deste, tamb�m dispara o evento Changed no invent�rio alvo.
        if (target != this) target.Changed?.Invoke();
        // Retorna true para indicar que a opera��o foi bem-sucedida.
        return true;
    }
}