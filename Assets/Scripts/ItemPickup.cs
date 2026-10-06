using UnityEngine;

public class ItemPickup : Interactable
{
    [SerializeField] private Item _item;
    [SerializeField, Min(1)] private int _amount = 1;

    protected override string DefaultPrompt => "pickup";

    protected override void OnInteract(GameObject interactor)
    {
        if (_item == null)
        {
            Debug.LogError($"{name}: campo 'item' vazio no Inspector", this);
            return;
        }

        PlayerHands hands = interactor.transform.root.GetComponentInChildren<PlayerHands>();
        if (hands == null)
        {
            Debug.LogError($"{name}: PlayerHands não encontrado a partir de '{interactor.name}'", this);
            return;
        }

        int left = hands.Inventory.Add(_item, _amount);

        if (left <= 0)
        {
            Destroy(gameObject);
        }
        else
        {
            _amount = left;
            Debug.Log("Mãos cheias.");
        }
    }
}
