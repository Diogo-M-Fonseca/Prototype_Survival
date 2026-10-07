using TMPro;
using UnityEngine;

[CreateAssetMenu(menuName = "UI/Style", fileName = "UIStyle")]
public class UIStyle : ScriptableObject
{
    [Header("Texto")]
    public TMP_FontAsset font;
    public float fontSize = 28f;
    public Color textColor = new Color(0.55f, 0.55f, 0.55f, 1f);
    public Color textSelectedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
    public Color textUnavailableColor = new Color(0.35f, 0.3f, 0.3f, 1f);
    public float textPadding = 32f;
    public float detailsFontSize = 22f;

    [Header("Linhas")]
    public float rowHeight = 78f;
    public float rowSpacing = 2f;
    public Color rowColor = new Color(0.29f, 0.29f, 0.29f, 1f);
    public Color rowSelectedColor = new Color(0.4f, 0.4f, 0.4f, 1f);
    public Color selectedBorderColor = new Color(0.8f, 0.8f, 0.8f, 0.9f);

    [Header("Painel da lista")]
    public Color panelColor = new Color(0.36f, 0.36f, 0.36f, 1f);

    [Tooltip("Sprite 9-slice opcional (cantos arredondados).")]
    public Sprite panelSprite;
    public float listWidth = 560f;

    [Header("Indicador de scroll")]
    public float indicatorWidth = 8f;
    public float indicatorGap = 14f;
    public Color indicatorTrackColor = new Color(1f, 1f, 1f, 0f);
    public Color indicatorColor = new Color(0.45f, 0.45f, 0.45f, 1f);
    public float RowStep => rowHeight + rowSpacing;
}
