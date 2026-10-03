using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    [SerializeField] private Item item;
    [SerializeField, Min(1)] private int amount = 1;

    private void OnTriggerEnter(Collider other)
    {
        var hand = other.GetComponentInChildren<PlayerHands>();
        if (hand == null) return;

        int left = hand.Inventory.Add(item, amount);
        if (left <= 0) Destroy(gameObject);
        else amount = left; // mãos cheias: o resto fica no chão
    }
}
