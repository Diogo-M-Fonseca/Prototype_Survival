using System;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class Inventory : MonoBehaviour, IInventory
{
    [SerializeField, Min(1)] private int _size = 20;

    [SerializeField, Min(0f)] private float _durabilityRate = 1f; 

    private ItemStack[] _slots;

    public event Action Changed;

    public int Size => _size;
    public float DurabilityRate => _durabilityRate;
    public ItemStack this[int i] => _slots[i];

    private void Awake() => _slots = new ItemStack[_size];

    private void Update()
    {
        if (_durabilityRate <= 0f) return;

        float decay = Time.deltaTime * _durabilityRate;
        bool changed = false;

        for (int i = 0; i < _slots.Length; i++)
        {
            ItemStack slot = _slots[i];
            if (slot.IsEmpty || !slot.Item.HasDurability) continue;

            float remaining = slot.DurabilityRemaining - decay;
            _slots[i] = remaining <= 0f
                ? default
                : slot.WithDurability(remaining);
            changed = true;
        }

        if (changed) Changed?.Invoke();
    }

    public int Add(Item item, int amount)
    {
        if (item == null || amount <= 0) return amount;

        int remaining = amount;

        for (int i = 0; i < _slots.Length && remaining > 0; i++)
        {
            if (_slots[i].IsEmpty || _slots[i].Item != item) continue;

            int add = Mathf.Min(remaining, item.MaxStack - _slots[i].Amount);
            if (add <= 0) continue;

            _slots[i] = _slots[i].WithAmount(_slots[i].Amount + add);
            remaining -= add;
        }

        for (int i = 0; i < _slots.Length && remaining > 0; i++)
        {
            if (!_slots[i].IsEmpty) continue;

            int add = Mathf.Min(remaining, item.MaxStack);
            _slots[i] = new ItemStack(item, add);
            remaining -= add;
        }

        if (remaining != amount) Changed?.Invoke();
        return remaining;
    }

    public bool CanAdd(Item item, int amount)
    {
        if (item == null) return false;

        foreach (ItemStack s in _slots)
        {
            if (amount <= 0) break;

            if (s.IsEmpty) amount -= item.MaxStack;
            else if (s.Item == item) amount -= item.MaxStack - s.Amount;
        }
        return amount <= 0;
    }

    public int Count(Item item)
    {
        if (item == null) return 0;

        int total = 0;
        foreach (ItemStack s in _slots)
            if (!s.IsEmpty && s.Item == item) total += s.Amount;
        return total;
    }

    public bool Has(Item item, int amount) => Count(item) >= amount;

    public bool Remove(Item item, int amount)
    {
        if (item == null || amount <= 0 || !Has(item, amount)) return false;

        for (int i = _slots.Length - 1; i >= 0 && amount > 0; i--)
        {
            if (_slots[i].IsEmpty || _slots[i].Item != item) continue;

            int take = Mathf.Min(amount, _slots[i].Amount);
            int left = _slots[i].Amount - take;
            _slots[i] = left <= 0 ? default : _slots[i].WithAmount(left);
            amount -= take;
        }

        Changed?.Invoke();
        return true;
    }

    public bool UseSlot(int index, GameObject user)
    {
        if ((uint)index >= (uint)_slots.Length) return false;

        ItemStack slot = _slots[index];
        if (slot.IsEmpty || !slot.Item.Use(user)) return false;

        int left = slot.Amount - 1;
        _slots[index] = left <= 0 ? default : slot.WithAmount(left);

        Changed?.Invoke();
        return true;
    }

    public bool TransferSlot(int index, Inventory target)
    {
        if (target == null || target == this) return false;
        if ((uint)index >= (uint)_slots.Length) return false;

        ItemStack s = _slots[index];
        if (s.IsEmpty) return false;
        if (!target.CanAdd(s.Item, s.Amount)) return false;

        if (!target.TryAddStack(s)) return false;

        _slots[index] = default;
        Changed?.Invoke();
        return true;
    }

    private bool TryAddStack(ItemStack stack)
    {
        if (stack.IsEmpty) return false;

        if (!stack.Item.HasDurability)
            return Add(stack.Item, stack.Amount) == 0;

        for (int i = 0; i < _slots.Length; i++)
        {
            if (!_slots[i].IsEmpty) continue;

            _slots[i] = stack;
            Changed?.Invoke();
            return true;
        }

        return false;
    }

    public bool MoveOrSwap(int from, Inventory target, int to)
    {
        if (target == null) return false;
        if ((uint)from >= (uint)_slots.Length || (uint)to >= (uint)target._slots.Length) return false;
        if (target == this && from == to) return false;

        ItemStack a = _slots[from];
        ItemStack b = target._slots[to];
        if (a.IsEmpty) return false;

        if (!b.IsEmpty && b.Item == a.Item)
        {
            int space = a.Item.MaxStack - b.Amount;
            if (space > 0)
            {
                int move = Mathf.Min(space, a.Amount);
                target._slots[to] = b.WithAmount(b.Amount + move);

                int left = a.Amount - move;
                _slots[from] = left <= 0 ? default : a.WithAmount(left);

                Changed?.Invoke();
                if (target != this) target.Changed?.Invoke();
                return true;
            }
        }

        _slots[from] = b;
        target._slots[to] = a;

        Changed?.Invoke();
        if (target != this) target.Changed?.Invoke();
        return true;
    }
}