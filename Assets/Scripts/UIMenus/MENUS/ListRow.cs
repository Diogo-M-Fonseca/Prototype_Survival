using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ListRow : MonoBehaviour, IPointerClickHandler
{
    private const float IconMargin = 10f;

    private Image _background;
    private TMP_Text _label;
    private Image _icon;
    private Outline _outline;
    private CanvasGroup _group;
    private UIStyle _style;
    private float _iconSize;

    public RectTransform Rect { get; private set; }

    public int BoundIndex { get; set; } = int.MinValue;

    public bool Available { get; private set; } = true;

    public event Action<ListRow> Clicked;

    public void Initialize(
        Image background,
        TMP_Text label,
        Image icon,
        Outline outline,
        CanvasGroup group,
        UIStyle style
    )
    {
        Rect = (RectTransform)transform;
        _background = background;
        _label = label;
        _icon = icon;
        _outline = outline;
        _group = group;
        _style = style;
        _iconSize = style.rowHeight - IconMargin * 2f;
    }

    public void Bind(string text, Sprite icon, bool available)
    {
        Available = available;
        _label.text = text;

        bool hasIcon = icon != null;
        _icon.enabled = hasIcon;
        _icon.sprite = icon;
        _icon.color = new Color(1f, 1f, 1f, available ? 1f : 0.4f);

        float left = _style.textPadding + (hasIcon ? _iconSize + IconMargin : 0f);
        _label.rectTransform.offsetMin = new Vector2(left, 0f);
    }

    public void ApplyVisual(float selection, float alpha)
    {
        Color text = Color.Lerp(_style.textColor, _style.textSelectedColor, selection);
        if (!Available)
            text = Color.Lerp(text, _style.textUnavailableColor, 0.75f);

        _label.color = text;
        _background.color = Color.Lerp(_style.rowColor, _style.rowSelectedColor, selection);

        Color border = _style.selectedBorderColor;
        border.a *= selection;
        if (_outline.effectColor != border)
            _outline.effectColor = border;

        _group.alpha = alpha;
    }

    public void OnPointerClick(PointerEventData eventData) => Clicked?.Invoke(this);
}
