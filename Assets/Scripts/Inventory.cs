using System;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class Inventory : MonoBehaviour, IInventory
{
    /// <summary>
    /// Numero total de slots no inventário.
    /// </summary>
    [SerializeField, Min(1)] private int _size = 20;

    /// <summary>
    /// Velocidade de desgaste: unidades de durabilidade perdidas por segundo.
    /// </summary>
    [SerializeField, Min(0f)] private float _durabilityRate = 1f;

    /// <summary>
    /// Intervalo minimo entre notificações de mudança de durabilidade. Isso evita que o evento Changed seja disparado com muita frequência.
    /// </summary>
    [SerializeField, Min(0.05f)] private float _durabilityNotifyInterval = 0.25f;

    /// <summary>
    /// Slots do inventário. 
    /// </summary>
    private ItemStack[] _slots;

    /// <summary>
    /// Tempo acumulado desde a última notificação de mudança de durabilidade. Quando atinge _durabilityNotifyInterval, o evento Changed é disparado.
    /// </summary>
    private float _notifyTimer;

    /// <summary>
    /// True se houver desgaste que ainda não foi notificado.
    /// </summary>
    private bool _durabilityDirty;

    /// <summary>
    /// Disparado sempre que o conteudo do inventário muda.
    /// </summary>
    public event Action Changed;

    /// <summary>
    /// Numero de slots
    /// </summary>
    public int Size => _size;

    /// <summary>
    /// Taxa de desgaste atual
    /// </summary>
    public float DurabilityRate => _durabilityRate;

    // Acesso só de leitura ao stack de um slot. Se o slot estiver vazio, retorna um ItemStack default.
    public ItemStack this[int i] => _slots[i];

    // Cria o array de slots no Awake, para garantir que ele seja inicializado antes de qualquer outro script tentar acessá-lo.
    private void Awake() => _slots = new ItemStack[_size];

    // Aplica o desgaste de durabilidade a cada frame, e dispara o evento Changed se algum item quebrar ou se o intervalo de notificação for atingido.
    private void Update()
    {
        // Desgaste desativado.
        if (_durabilityRate <= 0f) return;

        // Quanto desgaste cabe neste frame.
        float decay = Time.deltaTime * _durabilityRate;
        bool broke = false;

        // Aplica o desgaste a cada slot.
        for (int i = 0; i < _slots.Length; i++)
        {
            // Só interessam slots com item que tenha durabilidade.
            ItemStack slot = _slots[i];
            if (slot.IsEmpty || !slot.Item.HasDurability) continue;

            // Calcula a durabilidade restante após o desgaste.
            float remaining = slot.DurabilityRemaining - decay;

            if (remaining <= 0f)
            {
                // Item quebrou, remove do slot.
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

        // O desgaste aplica-se todos os frames, mas o UI só é avisado ás vezes.
        _notifyTimer += Time.deltaTime;
        if (broke || _notifyTimer >= _durabilityNotifyInterval)
        {
            _notifyTimer = 0f;
            _durabilityDirty = false;
            Changed?.Invoke();
        }
    }

    /// <summary>
    /// Adicona um item ao inventário. Retorna a quantidade que não coube no inventário.
    /// </summary>
    /// <param name="item">O item a ser adicionado.</param>
    /// <param name="amount">A quantidade do item a ser adicionada.</param>
    /// <returns>A quantidade de itens que não couberam no inventário.</returns>
    public int Add(Item item, int amount)
    {
        // Se o item for nulo ou a quantidade for menor ou igual a zero, não faz nada e retorna a quantidade original.
        if (item == null || amount <= 0) return amount;

        // Quantidade restante de itens que ainda precisam ser adicionados.
        int remaining = amount;

        // Primeiro, tenta adicionar aos slots que já possuem o mesmo item, respeitando o limite de empilhamento.
        for (int i = 0; i < _slots.Length && remaining > 0; i++)
        {
            if (_slots[i].IsEmpty || _slots[i].Item != item) continue;

            int add = Mathf.Min(remaining, item.MaxStack - _slots[i].Amount);
            if (add <= 0) continue;

            _slots[i] = _slots[i].WithAmount(_slots[i].Amount + add);
            remaining -= add;
        }

        // Em seguida, tenta adicionar aos slots vazios.
        for (int i = 0; i < _slots.Length && remaining > 0; i++)
        {
            if (!_slots[i].IsEmpty) continue;

            int add = Mathf.Min(remaining, item.MaxStack);
            _slots[i] = new ItemStack(item, add);
            remaining -= add;
        }

        // Se a quantidade restante for diferente da quantidade original, significa que houve uma mudança no inventário, então dispara o evento Changed.
        if (remaining != amount) Changed?.Invoke();
        // Retorna a quantidade de itens que não couberam no inventário.
        return remaining;
    }

    /// <summary>
    ///  Verfica se cabe a quantidade pedida sem a adicionar.
    /// </summary>
    /// <param name="item"></param>
    /// <param name="amount"></param>
    /// <returns></returns>
    public bool CanAdd(Item item, int amount)
    {
        // Se o item for nulo, não cabe.
        if (item == null) return false;

        // Vai descontando o espaço disponivel à qauntidade pedida.
        foreach (ItemStack s in _slots)
        {
            if (amount <= 0) break;

            // Slot vazio: cabe um stack inteiro.
            if (s.IsEmpty) amount -= item.MaxStack;
            // mesmo item: cabe o que falta para encher.
            else if (s.Item == item) amount -= item.MaxStack - s.Amount;
        }
        return amount <= 0;
    }

    /// <summary>
    /// Conta quantas unidades de um item existem no inventário somando todos os stacks.
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
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
    /// Indica se existe pelo menos a quantidade pedida do item no inventário.
    /// </summary>
    /// <param name="item"></param>
    /// <param name="amount"></param>
    /// <returns></returns>
    public bool Has(Item item, int amount) => Count(item) >= amount;

    /// <summary>
    /// Remove uma quantidade de um item, começando pelos ultimos slots.
    /// </summary>
    /// <param name="item"></param>
    /// <param name="amount"></param>
    /// <returns>true se removeu; false se não havia quantidade suficiente.</returns>
    public bool Remove(Item item, int amount)
    {
        // Se o item for nulo, a quantidade for menor ou igual a zero, ou não houver quantidade suficiente, não faz nada e retorna false.
        if (item == null || amount <= 0 || !Has(item, amount)) return false;

        // percorre de trás pra frente, retirando de cada stack até cumprir a quantidade.
        for (int i = _slots.Length - 1; i >= 0 && amount > 0; i--)
        {
            // Se o slot estiver vazio ou não for o item procurado, pula para o próximo.
            if (_slots[i].IsEmpty || _slots[i].Item != item) continue;

            // Calcula quanto pode retirar deste slot, que é o mínimo entre a quantidade restante e a quantidade no slot.
            int take = Mathf.Min(amount, _slots[i].Amount);
            int left = _slots[i].Amount - take;
            _slots[i] = left <= 0 ? default : _slots[i].WithAmount(left);
            amount -= take;
        }

        // Dispara o evento Changed para notificar que o inventário mudou.
        Changed?.Invoke();
        // Retorna true para indicar que a remoção foi bem-sucedida.
        return true;
    }

    /// <summary>
    /// Usa o item de um slot, se o uso tiver sucesso, consome uma unidade.
    /// </summary>
    /// <param name="index"></param>
    /// <param name="user"></param>
    /// <returns>true se o item foi usado.</returns>
    public bool UseSlot(int index, GameObject user)
    {
        // Se o index for inválido, retorna false.
        if ((uint)index >= (uint)_slots.Length) return false;

        // Pega o item do slot. Se estiver vazio ou o uso falhar, retorna false.
        ItemStack slot = _slots[index];
        if (slot.IsEmpty || !slot.Item.Use(user)) return false;

        // Consome uma unidade do item. Se não sobrar nenhuma, o slot fica vazio.
        int left = slot.Amount - 1;
        _slots[index] = left <= 0 ? default : slot.WithAmount(left);

        // Dispara o evento Changed para notificar que o inventário mudou.
        Changed?.Invoke();
        // Retorna true para indicar que o item foi usado com sucesso.
        return true;
    }

    /// <summary>
    /// Move o stack inteiro de um slot para outro inventário, se couber. Se não couber, não faz nada.
    /// </summary>
    /// <param name="index"></param>
    /// <param name="target"></param>
    /// <returns>true se transferiu.</returns>
    public bool TransferSlot(int index, Inventory target)
    {
        // Se o inventário alvo for nulo, ou for o mesmo inventário, ou o index for inválido, retorna false.
        if (target == null || target == this) return false;
        if ((uint)index >= (uint)_slots.Length) return false;

        // Pega o item do slot. Se estiver vazio, retorna false.
        ItemStack s = _slots[index];
        if (s.IsEmpty) return false;

        // Só transfere se o destino tiver espaço.
        if (!target.CanAdd(s.Item, s.Amount)) return false;
        if (!target.TryAddStack(s)) return false;

        // Só limpa a origem depois de o destino ter aceitado.
        _slots[index] = default;
        Changed?.Invoke();
        return true;
    }

    /// <summary>
    /// Adiciona um stack inteiro. Itens com durabilidade são copiados tal como estão para um slot vazio, os outros usam add normal.
    /// </summary>
    /// <param name="stack"></param>
    /// <returns>true se o stack foi adicionado com sucesso.</returns>
    private bool TryAddStack(ItemStack stack)
    {
        // Se o stack estiver vazio, não faz nada e retorna false.
        if (stack.IsEmpty) return false;

        // Se o item não tiver durabilidade, usa o método Add normal, que empilha.
        if (!stack.Item.HasDurability)
            return Add(stack.Item, stack.Amount) == 0;

        // Procura um slot vazio para colocar o stack inteiro.
        for (int i = 0; i < _slots.Length; i++)
        {
            // Se o slot não estiver vazio, pula para o próximo.
            if (!_slots[i].IsEmpty) continue;

            // Coloca o stack inteiro no slot vazio.
            _slots[i] = stack;
            // Dispara o evento Changed para notificar que o inventário mudou.
            Changed?.Invoke();
            // Retorna true para indicar que o stack foi adicionado com sucesso.
            return true;
        }

        // Se não encontrou nenhum slot vazio, retorna false.
        return false;
    }

    /// <summary>
    /// Move um stack para um slot. Se o destino tiver o mesmo item, junta o que couber, caso contrário troca os dois slots.
    /// </summary>
    /// <param name="from">Slot de origem neste inventário.</param>
    /// <param name="target">Inventário de destino (pode ser este).</param>
    /// <param name="to">Slot de destino.</param>
    /// <returns>true se algo mudou.</returns>
    public bool MoveOrSwap(int from, Inventory target, int to)
    {
        // Se o inventário alvo for nulo, ou os índices forem inválidos, ou se for o mesmo slot, retorna false.
        if (target == null) return false;
        if ((uint)from >= (uint)_slots.Length || (uint)to >= (uint)target._slots.Length) return false;
        if (target == this && from == to) return false;

        // Pega os stacks dos slots de origem e destino.
        ItemStack a = _slots[from];
        ItemStack b = target._slots[to];
        // Se o slot de origem estiver vazio, não há nada para mover, retorna false.
        if (a.IsEmpty) return false;

        // Mesmo item e ainda há espaço no destino: junta.
        if (!b.IsEmpty && b.Item == a.Item)
        {
            // Calcula quanto espaço ainda há no stack de destino.
            int space = a.Item.MaxStack - b.Amount;

            // Se houver espaço, move o que couber.
            if (space > 0)
            {
                // Calcula quanto pode mover, que é o mínimo entre o espaço disponível e a quantidade no slot de origem.
                int move = Mathf.Min(space, a.Amount);
                target._slots[to] = b.WithAmount(b.Amount + move);

                // Atualiza o slot de origem com a quantidade restante. Se não sobrar nada, o slot fica vazio.
                int left = a.Amount - move;
                _slots[from] = left <= 0 ? default : a.WithAmount(left);

                // Dispara o evento Changed para notificar que o inventário mudou.
                Changed?.Invoke();

                // Se o inventário alvo for diferente deste, também dispara o evento Changed no inventário alvo.
                if (target != this) target.Changed?.Invoke();

                // Retorna true para indicar que a operação foi bem-sucedida.
                return true;
            }
        }

        // Se não puder juntar, troca os dois slots.
        _slots[from] = b;
        target._slots[to] = a;

        // Dispara o evento Changed para notificar que o inventário mudou.
        Changed?.Invoke();
        // Se o inventário alvo for diferente deste, também dispara o evento Changed no inventário alvo.
        if (target != this) target.Changed?.Invoke();
        // Retorna true para indicar que a operação foi bem-sucedida.
        return true;
    }
}