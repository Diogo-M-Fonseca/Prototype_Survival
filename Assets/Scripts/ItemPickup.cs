using UnityEngine;

public class ItemPickup : MonoBehaviour, IInteractable
{
    [SerializeField] private Item item;
    [SerializeField, Min(1)] private int amount = 1;

    public void Interact()
    {
        if (item == null || PlayerHands.Instance == null) return;

        int left = PlayerHands.Instance.Inventory.Add(item, amount);

        if (left <= 0)
        {
            Destroy(gameObject);
        }
        else
        {
            amount = left;
            Debug.Log("Mãos cheias.");
        }
    }
}
