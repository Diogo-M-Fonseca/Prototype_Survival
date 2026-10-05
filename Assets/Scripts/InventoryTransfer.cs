using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryTransfer : MonoBehaviour
{
    [SerializeField] private InventoryGridUI _handUI;
    [SerializeField] private InventoryGridUI _bagUI;
    [SerializeField] private GameObject _bagPanel;
    [SerializeField] private Vector2 _ghostSize = new Vector2(100f, 100f);

    private RectTransform _ghost;
    private Image _ghostImage;
    private InventoryGridUI _sourceGrid;
    private int _sourceIndex = -1;

    private bool Dragging => _sourceGrid != null;

    private void Awake() => CreateGhost();

    private void CreateGhost()
    {
        GameObject go = new GameObject("DragGhost", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        _ghost = go.GetComponent<RectTransform>();
        _ghost.SetParent(transform, false);
        _ghost.sizeDelta = _ghostSize;

        go.GetComponent<CanvasGroup>().blocksRaycasts = false; // não bloqueia o drop

        _ghostImage = go.GetComponent<Image>();
        _ghostImage.raycastTarget = false;
        _ghostImage.preserveAspect = true;

        go.SetActive(false);
    }

    private void OnEnable()
    {
        foreach (InventoryGridUI grid in new[] { _handUI, _bagUI })
        {
            grid.BeginDrag += HandleBegin;
            grid.Drag += HandleDrag;
            grid.EndDrag += CancelDrag;
            grid.Drop += HandleDrop;
        }
    }

    private void OnDisable()
    {
        foreach (InventoryGridUI grid in new[] { _handUI, _bagUI })
        {
            grid.BeginDrag -= HandleBegin;
            grid.Drag -= HandleDrag;
            grid.EndDrag -= CancelDrag;
            grid.Drop -= HandleDrop;
        }
        CancelDrag();
    }

    private void Update()
    {
        if (Dragging && !_bagPanel.activeSelf) CancelDrag();
    }

    private void HandleBegin(InventoryGridUI grid, int index, PointerEventData e)
    {
        if (!_bagPanel.activeSelf) return;

        var stack = grid.Inventory[index];
        if (stack.IsEmpty) return;

        _sourceGrid = grid;
        _sourceIndex = index;
        grid.SetMarked(index);

        Sprite sprite = stack.Item.Icon;
        _ghostImage.sprite = sprite;
        _ghostImage.color = sprite != null ? Color.white : new Color(1f, 1f, 1f, 0.5f);

        _ghost.position = e.position;
        _ghost.gameObject.SetActive(true);
        _ghost.SetAsLastSibling();
    }

    private void HandleDrag(PointerEventData e)
    {
        if (Dragging) _ghost.position = e.position;
    }

    private void HandleDrop(InventoryGridUI target, int index)
    {
        if (!Dragging) return;
        _sourceGrid.Inventory.MoveOrSwap(_sourceIndex, target.Inventory, index);
    }

    private void CancelDrag()
    {
        if (_sourceGrid != null) _sourceGrid.SetMarked(-1);
        _sourceGrid = null;
        _sourceIndex = -1;
        if (_ghost != null) _ghost.gameObject.SetActive(false);
    }
}
