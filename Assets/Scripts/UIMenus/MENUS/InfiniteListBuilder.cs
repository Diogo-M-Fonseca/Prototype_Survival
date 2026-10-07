using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Monta por código toda a hierarquia de uma InfiniteList:
/// painel, viewport com máscara e indicador de scroll à direita.
/// </summary>
public static class InfiniteListBuilder
{
    public static InfiniteList Create(
        Transform parent,
        UIStyle style,
        int visibleRows = 6,
        int selectedSlot = 2,
        bool loop = true,
        string name = "InfiniteList"
    )
    {
        float height = visibleRows * style.RowStep - style.rowSpacing;
        float width = style.listWidth + style.indicatorGap + style.indicatorWidth;

        RectTransform root = UIFactory.CreateRect(name, parent);
        root.sizeDelta = new Vector2(width, height);

        // Painel (também serve de fundo/divisórias entre linhas)
        Image panel = UIFactory.CreateImage(
            root,
            "Panel",
            style.panelColor,
            style.panelSprite,
            true
        );
        RectTransform panelRect = panel.rectTransform;
        panelRect.anchorMin = new Vector2(0f, 0f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 0.5f);
        panelRect.sizeDelta = new Vector2(style.listWidth, 0f);
        panelRect.anchoredPosition = Vector2.zero;

        // Viewport com máscara: as linhas cortam-se nos limites
        RectTransform viewport = UIFactory.CreateRect("Viewport", panelRect);
        UIFactory.Stretch(viewport);
        viewport.gameObject.AddComponent<RectMask2D>();

        // Indicador de scroll
        Image track = UIFactory.CreateImage(root, "IndicatorTrack", style.indicatorTrackColor);
        RectTransform trackRect = track.rectTransform;
        trackRect.anchorMin = new Vector2(1f, 0f);
        trackRect.anchorMax = new Vector2(1f, 1f);
        trackRect.pivot = new Vector2(1f, 0.5f);
        trackRect.sizeDelta = new Vector2(style.indicatorWidth, 0f);
        trackRect.anchoredPosition = Vector2.zero;

        Image handle = UIFactory.CreateImage(
            trackRect,
            "Handle",
            style.indicatorColor,
            style.panelSprite
        );
        RectTransform handleRect = handle.rectTransform;
        handleRect.anchorMin = new Vector2(0f, 1f);
        handleRect.anchorMax = new Vector2(1f, 1f);
        handleRect.pivot = new Vector2(0.5f, 1f);
        handleRect.sizeDelta = new Vector2(0f, 40f);
        handleRect.anchoredPosition = Vector2.zero;

        InfiniteList list = root.gameObject.AddComponent<InfiniteList>();
        list.Configure(style, viewport, visibleRows, selectedSlot, loop);
        list.SetIndicator(trackRect, handleRect);
        return list;
    }
}
