using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

public static class UIFactory
{
    public static Canvas CreateCanvas(
        string name,
        int sortingOrder = 0,
        Vector2? referenceResolution = null
    )
    {
        GameObject go = new GameObject(
            name,
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster)
        );

        Canvas canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = referenceResolution ?? new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        EnsureEventSystem();
        return canvas;
    }

    public static void EnsureEventSystem()
    {
        if (EventSystem.current != null)
            return;

        GameObject es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
        es.AddComponent<InputSystemUIInputModule>();
#else
        es.AddComponent<StandaloneInputModule>();
#endif
    }

    public static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        return rt;
    }

    public static void Stretch(
        RectTransform rt,
        float left = 0f,
        float top = 0f,
        float right = 0f,
        float bottom = 0f
    )
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, -top);
    }

    public static void Place(RectTransform rt, Vector2 anchor, Vector2 size, Vector2 position)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.sizeDelta = size;
        rt.anchoredPosition = position;
    }

    public static Image CreateImage(
        Transform parent,
        string name,
        Color color,
        Sprite sprite = null,
        bool raycastTarget = false
    )
    {
        RectTransform rt = CreateRect(name, parent);
        Image image = rt.gameObject.AddComponent<Image>();
        image.color = color;
        image.sprite = sprite;
        image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
        image.raycastTarget = raycastTarget;
        return image;
    }

    public static TextMeshProUGUI CreateText(
        Transform parent,
        string name,
        string text,
        UIStyle style,
        Color color,
        float? fontSize = null,
        TextAlignmentOptions alignment = TextAlignmentOptions.MidlineLeft
    )
    {
        RectTransform rt = CreateRect(name, parent);
        TextMeshProUGUI tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (style.font != null)
            tmp.font = style.font;
        tmp.text = text;
        tmp.color = color;
        tmp.fontSize = fontSize ?? style.fontSize;
        tmp.alignment = alignment;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        tmp.raycastTarget = false;
        return tmp;
    }

    public static UIPanel CreatePanel(Transform parent, string name, Vector2 size)
    {
        RectTransform rt = CreateRect(name, parent);
        rt.gameObject.AddComponent<CanvasGroup>();
        Place(rt, new Vector2(0.5f, 0.5f), size, Vector2.zero);
        return rt.gameObject.AddComponent<UIPanel>();
    }

    public static ListRow CreateRow(Transform parent, UIStyle style)
    {
        RectTransform rt = CreateRect("Row", parent);
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(0f, style.rowHeight);

        Image background = rt.gameObject.AddComponent<Image>();
        background.color = style.rowColor;
        background.raycastTarget = true;

        Outline outline = rt.gameObject.AddComponent<Outline>();
        outline.effectDistance = new Vector2(1f, -1f);
        outline.effectColor = Color.clear;

        CanvasGroup group = rt.gameObject.AddComponent<CanvasGroup>();

        float iconSize = style.rowHeight - 20f;
        Image icon = CreateImage(rt, "Icon", Color.white);
        icon.preserveAspect = true;
        icon.enabled = false;
        RectTransform iconRect = icon.rectTransform;
        iconRect.anchorMin = new Vector2(0f, 0.5f);
        iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(0f, 0.5f);
        iconRect.sizeDelta = new Vector2(iconSize, iconSize);
        iconRect.anchoredPosition = new Vector2(style.textPadding * 0.5f, 0f);

        TextMeshProUGUI label = CreateText(rt, "Label", string.Empty, style, style.textColor);
        Stretch(label.rectTransform, style.textPadding, 0f, style.textPadding, 0f);

        ListRow row = rt.gameObject.AddComponent<ListRow>();
        row.Initialize(background, label, icon, outline, group, style);
        return row;
    }
}
