using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Items/Inventory Item")]
public class FoodsAndDrinks : Item
{
    [SerializeField] private RestoreStat statToRestore = RestoreStat.Thirst;
    [SerializeField, Min(0)] private int amountRestored;

    public override bool Use(GameObject user)
    {
        if (amountRestored <= 0 || user == null) return false;

        PlayerStats stats = user.GetComponent<PlayerStats>();
        if (stats == null) stats = user.GetComponentInParent<PlayerStats>();
        if (stats == null) return false;

        switch (statToRestore)
        {
            case RestoreStat.Hunger:
                return stats.RestoreHunger(amountRestored);
            case RestoreStat.Health:
                return stats.RestoreHealth(amountRestored);
            case RestoreStat.Thirst:
                return stats.RestoreThirst(amountRestored);
            default:
                return false;
        }
    }
}
