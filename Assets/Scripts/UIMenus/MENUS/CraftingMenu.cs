using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;


public class CraftingMenu : MonoBehaviour
{
    private const float DetailsGap = 16f;
    private const float DetailsHeight = 120f;
    private const string CannotCraftMessage = "<color=#C96A6A>Não é possível criar (faltam ingredientes ou espaço).</color>";

    [SerializeField] private UIStyle _style;
    [SerializeField] private Recipe[] _recipes;

    [Tooltip("Canvas onde criar o menu. Se vazio, cria um novo.")]
    [SerializeField] private Canvas _canvas;

    [SerializeField, Min(1)] private int _visibleRows = 6;
    [SerializeField, Min(0)] private int _selectedSlot = 2;

    [Tooltip("Mostra e liberta o cursor enquanto o menu está aberto, e restaura-o ao fechar.")]
    [SerializeField] private bool _manageCursor = true;

    [Tooltip("Deslocamento do menu em pixels (ex.: x positivo para ficar do lado direito, deixando o esquerdo ao inventário).")]
    [SerializeField] private Vector2 _menuOffset = new Vector2(300f, 0f);

    [Tooltip("Pára o jogo (Time.timeScale = 0) enquanto o menu está aberto.")]
    [SerializeField] private bool _pauseGame = true;

    private readonly List<Recipe> _valid = new List<Recipe>();

    private UIPanel _panel;
    private InfiniteList _list;
    private TMP_Text _details;
    private IInventory _inventory;
    private UIStyle _createdStyle;
    private int _lastIndex;
    private Recipe[] _currentRecipes;
    private bool _open;
    private CursorLockMode _previousLock;
    private bool _previousVisible;
    private float _previousTimeScale = 1f;

    public event Action<Recipe> Crafted;

 
    public event Action<bool> VisibilityChanged;

    public static bool AnyOpen { get; private set; }

    public bool IsOpen => _panel != null && _panel.IsVisible;

  
    public bool ManageCursor
    {
        get => _manageCursor;
        set => _manageCursor = value;
    }

    private void Awake()
    {
        if (_style == null) _style = _createdStyle = ScriptableObject.CreateInstance<UIStyle>();

        Build();
        _panel.SetVisible(false, true);
        _panel.VisibilityChanged += OnVisibilityChanged; 
    }

    private void OnDisable()
    {
        if (_open) Close();
    }

    private void OnDestroy()
    {
        Unbind();
        if (_open) ApplyOpenState(false);
        if (_createdStyle != null) Destroy(_createdStyle);
    }

    public void Open(IInventory inventory) => Open(inventory, null);

    public void Open(IInventory inventory, Recipe[] recipes)
    {
        Unbind();

        Recipe[] source = recipes ?? _recipes;
        if (!ReferenceEquals(source, _currentRecipes))
        {
            _currentRecipes = source;
            _lastIndex = 0;
        }

        _inventory = inventory;
        if (_inventory != null) _inventory.Changed += Refresh;

        _valid.Clear();
        if (source != null)
            foreach (Recipe r in source)
                if (r != null) _valid.Add(r);

        _list.InputEnabled = true;
        _list.SetData(_valid.Count,
            i => LabelOf(_valid[i]),
            i => _valid[i].CanCraft(_inventory),
            i => _valid[i] is IRecipeDisplay d ? d.Icon : null,
            Mathf.Clamp(_lastIndex, 0, Mathf.Max(0, _valid.Count - 1)));

        UpdateDetails(_list.SelectedIndex);
        _panel.Show();
        ApplyOpenState(true);
    }

    public void Close()
    {
        _list.InputEnabled = false;
        _panel.Hide();
        ApplyOpenState(false);
    }
    public void Refresh()
    {
        if (!_open) return;

        _list.Refresh();
        UpdateDetails(_list.SelectedIndex);
    }

    private void OnVisibilityChanged(bool visible)
    {
        if (!visible)
        {
            Unbind();
            ApplyOpenState(false);
        }
    }

    private void ApplyOpenState(bool open)
    {
        if (_open == open) return;
        _open = open;
        AnyOpen = open;

        if (_pauseGame)
        {
            if (open)
            {
                _previousTimeScale = Time.timeScale;
                Time.timeScale = 0f;
            }
            else
            {
                Time.timeScale = _previousTimeScale;
            }
        }

        if (_manageCursor)
        {
            if (open)
            {
                _previousLock = Cursor.lockState;
                _previousVisible = Cursor.visible;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = _previousLock;
                Cursor.visible = _previousVisible;
            }
        }

        VisibilityChanged?.Invoke(open);
    }

    private void Unbind()
    {
        if (_inventory != null) _inventory.Changed -= Refresh;
    }

    private void Build()
    {
        if (_canvas == null) _canvas = UIFactory.CreateCanvas("CraftingCanvas", 10);

        float listHeight = _visibleRows * _style.RowStep - _style.rowSpacing;
        float width = _style.listWidth + _style.indicatorGap + _style.indicatorWidth;

        _panel = UIFactory.CreatePanel(_canvas.transform, "CraftingMenu",
            new Vector2(width, listHeight + DetailsGap + DetailsHeight));
        ((RectTransform)_panel.transform).anchoredPosition = _menuOffset;

        _list = InfiniteListBuilder.Create(_panel.transform, _style, _visibleRows, _selectedSlot);
        UIFactory.Place(_list.Root, new Vector2(0.5f, 1f), _list.Root.sizeDelta, Vector2.zero);

        _details = UIFactory.CreateText(_panel.transform, "Details", string.Empty, _style, _style.textColor,
            _style.detailsFontSize, TextAlignmentOptions.TopLeft);
        UIFactory.Place(_details.rectTransform, new Vector2(0.5f, 1f),
            new Vector2(_style.listWidth, DetailsHeight), new Vector2(0f, -(listHeight + DetailsGap)));

        _list.SelectionChanged += OnSelectionChanged;
        _list.Submitted += TryCraft;
    }

    private void OnSelectionChanged(int index)
    {
        if (index >= 0) _lastIndex = index;
        UpdateDetails(index);
    }

    private void TryCraft(int index)
    {
        if (index < 0 || index >= _valid.Count) return;

        Recipe recipe = _valid[index];

        if (recipe.Craft(_inventory))
        {
            Crafted?.Invoke(recipe);
            Refresh();
        }
        else
        {
            _details.text = CannotCraftMessage;
        }
    }

    private void UpdateDetails(int index)
    {
        if (index < 0 || index >= _valid.Count)
        {
            _details.text = string.Empty;
            return;
        }

        _details.text = _valid[index] is IRecipeDisplay d ? d.GetDetails(_inventory) : string.Empty;
    }

    private static string LabelOf(Recipe recipe)
    {
        return recipe is IRecipeDisplay d ? d.DisplayName : recipe.name;
    }
}
