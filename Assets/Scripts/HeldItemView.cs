using UnityEngine;

public class HeldItemView : MonoBehaviour
{
    [SerializeField] private Inventory hands;
    [SerializeField] private HotbarController hotbar;
    [SerializeField] private Transform handSlot;

    private GameObject current;

    private void OnEnable()
    {
        hotbar.SelectionChanged += _ => Refresh();
        hands.Changed += Refresh;
    }

    private void OnDisable()
    {
        hotbar.SelectionChanged -= _ => Refresh();
        hands.Changed -= Refresh;
    }

    private void Refresh()
    {
        if (current != null) Destroy(current);

        int i = hotbar.Selected;
        if (i < 0 || hands[i].IsEmpty) return;

        var prefab = hands[i].Item.HeldPrefab;
        if (prefab != null) current = Instantiate(prefab, handSlot);
    }
}
