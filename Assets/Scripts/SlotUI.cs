using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class SlotUI : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text amountText;
    [SerializeField] private GameObject highlight;

    private int index;
    private Action<int> onClick;

    public void Init(int index, Action<int> onClick)
    {
        this.index = index;
        this.onClick = onClick;
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

    public void OnPointerClick(PointerEventData eventData) => onClick?.Invoke(index);
}
