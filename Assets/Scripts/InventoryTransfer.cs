using UnityEngine;

public class InventoryTransfer : MonoBehaviour
{
    [SerializeField] private Inventory hand;
    [SerializeField] private Inventory bag;
    [SerializeField] private InventoryGridUI handUI;
    [SerializeField] private InventoryGridUI bagUI;
    [SerializeField] private GameObject bagPanel;

    private void OnEnable()
    {
        bagUI.SlotClicked += BagToHand;
        handUI.SlotClicked += HandToBag;
    }

    private void OnDisable()
    {
        bagUI.SlotClicked -= BagToHand;
        handUI.SlotClicked -= HandToBag;
    }

    private void BagToHand(int index) => bag.TransferSlot(index, hand);

    private void HandToBag(int index)
    {
        if (bagPanel.activeSelf) hand.TransferSlot(index, bag);
    }
}
