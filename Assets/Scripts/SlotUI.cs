using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class SlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [SerializeField] private Image _icon;
    [SerializeField] private TMP_Text _amountText;
    [SerializeField] private GameObject _highlight;
    [SerializeField] private Color _markedColor = new Color(1f, 0.85f, 0.3f, 0.8f);

    private Image _background;
    private Color _baseColor;
    private InventoryGridUI _owner;
    private int _index;

    private void Awake()
    {
        _background = GetComponent<Image>();
        if (_background != null) _baseColor = _background.color;
        SetSelected(false);
    }

    public void Init(InventoryGridUI owner, int index)
    {
        this._owner = owner;
        this._index = index;
    }

    public void Set(ItemStack stack)
    {
        bool empty = stack.IsEmpty;
        Sprite sprite = empty ? null : stack.Item.Icon;

        _icon.sprite = sprite;
        _icon.enabled = sprite != null;
        _amountText.text = (!empty && stack.Amount > 1) ? stack.Amount.ToString() : "";
    }

    public void SetSelected(bool selected)
    {
        if (_highlight != null) _highlight.SetActive(selected);
    }

    public void SetMarked(bool marked)
    {
        if (_background != null) _background.color = marked ? _markedColor : _baseColor;
    }

    public void OnBeginDrag(PointerEventData e) => _owner.NotifyBeginDrag(_index, e);
    public void OnDrag(PointerEventData e) => _owner.NotifyDrag(e);
    public void OnEndDrag(PointerEventData e) => _owner.NotifyEndDrag();
    public void OnDrop(PointerEventData e) => _owner.NotifyDrop(_index);
}
