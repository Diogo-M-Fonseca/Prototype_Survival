using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Items/Foods and Drinks")]
public class FoodsAndDrinks : Item
{
    private const int MaxRestores = 3;
    [SerializeField] private StatRestore[] _restores = new StatRestore[1];
    public int StatCount => _restores != null ? _restores.Length : 0;

    public override bool Use(GameObject user)
    {
        if (user == null || _restores == null) return false;

        PlayerStats stats = user.GetComponentInParent<PlayerStats>();
        if (stats == null) return false;

        bool used = false;
        foreach (StatRestore r in _restores)
        {
            if (r.Amount <= 0) continue;

            switch (r.Stat)
            {
                case RestoreStat.Hunger: used |= stats.RestoreHunger(r.Amount); break;
                case RestoreStat.Health: used |= stats.RestoreHealth(r.Amount); break;
                case RestoreStat.Thirst: used |= stats.RestoreThirst(r.Amount); break;
            }
        }
        return used;
    }

    protected override void OnValidate()
    {
        base.OnValidate();

        if (_restores != null && _restores.Length > MaxRestores)
            System.Array.Resize(ref _restores, MaxRestores);
    }
}
