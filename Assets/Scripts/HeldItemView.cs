using UnityEngine;

public class HeldItemView : MonoBehaviour
{
    [SerializeField] private Inventory _hands;
    [SerializeField] private HotbarController _hotbar;
    [SerializeField] private Transform _handSlot;

    private GameObject _current;
    private Item _currentItem;

    private void OnEnable()
    {
        _hotbar.SelectionChanged += OnSelectionChanged;
        _hands.Changed += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        _hotbar.SelectionChanged -= OnSelectionChanged;
        _hands.Changed -= Refresh;
    }

    private void OnSelectionChanged(int _) => Refresh();

    private void Refresh()
    {
        int i = _hotbar.Selected;
        Item item = (i < 0 || _hands[i].IsEmpty) ? null : _hands[i].Item;

        if (item == _currentItem) return;   // nada mudou, não recria
        _currentItem = item;

        if (_current != null) Destroy(_current);
        _current = null;

        if (item == null || item.HeldPrefab == null) return;

        _current = Instantiate(item.HeldPrefab, _handSlot);
        _current.transform.localPosition = Vector3.zero;
        _current.transform.localRotation = Quaternion.identity;

        foreach (Collider c in _current.GetComponentsInChildren<Collider>())
            c.enabled = false;
        foreach (Rigidbody rb in _current.GetComponentsInChildren<Rigidbody>())
            rb.isKinematic = true;
    }
}
