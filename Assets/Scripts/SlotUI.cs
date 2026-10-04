using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class SlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text amountText;
    [SerializeField] private GameObject highlight;
    [SerializeField] private Color markedColor = new Color(1f, 0.85f, 0.3f, 0.8f);

    private Image background;
    private Color baseColor;
    private InventoryGridUI owner;
    private int index;

    private void Awake()
    {
        background = GetComponent<Image>();
        if (background != null) baseColor = background.color;
        SetSelected(false);
    }

    public void Init(InventoryGridUI owner, int index)
    {
        this.owner = owner;
        this.index = index;
    }

    public void Set(ItemStack stack)
    {
        bool empty = stack.IsEmpty;
        Sprite sprite = empty ? null : stack.Item.Icon;

        icon.sprite = sprite;
        icon.enabled = sprite != null;
        amountText.text = (!empty && stack.Amount > 1) ? stack.Amount.ToString() : "";
    }

    public void SetSelected(bool selected)
    {
        if (highlight != null) highlight.SetActive(selected);
    }

    public void SetMarked(bool marked)
    {
        if (background != null) background.color = marked ? markedColor : baseColor;
    }

    public void OnBeginDrag(PointerEventData e) => owner.NotifyBeginDrag(index, e);
    public void OnDrag(PointerEventData e) => owner.NotifyDrag(e);
    public void OnEndDrag(PointerEventData e) => owner.NotifyEndDrag();
    public void OnDrop(PointerEventData e) => owner.NotifyDrop(index);
}
