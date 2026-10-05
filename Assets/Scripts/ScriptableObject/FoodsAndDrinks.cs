using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Items/Foods and Drinks")]
public class FoodsAndDrinks : Item
{
    [SerializeField, Range(1, 3)] private int _statCount = 1;
    [SerializeField] private RestoreStat[] _statsToRestore = new RestoreStat[1];
    [SerializeField] private int[] _amountsRestored = new int[1];

    public int StatCount => _statCount;

    public override bool Use(GameObject user)
    {
        if (user == null) return false;

        PlayerStats stats = user.GetComponentInParent<PlayerStats>();
        if (stats == null) return false;

        bool used = false;
        for (int i = 0; i < _statCount; i++)
        {
            int amount = _amountsRestored[i];
            if (amount <= 0) continue;

            switch (_statsToRestore[i])
            {
                case RestoreStat.Hunger: used |= stats.RestoreHunger(amount); break;
                case RestoreStat.Health: used |= stats.RestoreHealth(amount); break;
                case RestoreStat.Thirst: used |= stats.RestoreThirst(amount); break;
            }
        }
        return used;
    }

    private void OnValidate()
    {
        _statCount = Mathf.Clamp(_statCount, 1, 3);

        if (_statsToRestore == null || _statsToRestore.Length != _statCount)
        {
            System.Array.Resize(ref _statsToRestore, _statCount);
        }
        if (_amountsRestored == null || _amountsRestored.Length != _statCount)
        {
            System.Array.Resize(ref _amountsRestored, _statCount);
        }
    }
}
