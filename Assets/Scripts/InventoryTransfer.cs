using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryTransfer : MonoBehaviour
{
    [SerializeField] private InventoryGridUI handUI;
    [SerializeField] private InventoryGridUI bagUI;
    [SerializeField] private GameObject bagPanel;
    [SerializeField] private Vector2 ghostSize = new Vector2(100f, 100f);

    private RectTransform ghost;
    private Image ghostImage;
    private InventoryGridUI sourceGrid;
    private int sourceIndex = -1;

    private bool Dragging => sourceGrid != null;

    private void Awake() => CreateGhost();

    private void CreateGhost()
    {
        GameObject go = new GameObject("DragGhost", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        ghost = go.GetComponent<RectTransform>();
        ghost.SetParent(transform, false);
        ghost.sizeDelta = ghostSize;

        go.GetComponent<CanvasGroup>().blocksRaycasts = false; // não bloqueia o drop

        ghostImage = go.GetComponent<Image>();
        ghostImage.raycastTarget = false;
        ghostImage.preserveAspect = true;

        go.SetActive(false);
    }

    private void OnEnable()
    {
        foreach (InventoryGridUI grid in new[] { handUI, bagUI })
        {
            grid.BeginDrag += HandleBegin;
            grid.Drag += HandleDrag;
            grid.EndDrag += CancelDrag;
            grid.Drop += HandleDrop;
        }
    }

    private void OnDisable()
    {
        foreach (InventoryGridUI grid in new[] { handUI, bagUI })
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
        if (Dragging && !bagPanel.activeSelf) CancelDrag();
    }

    private void HandleBegin(InventoryGridUI grid, int index, PointerEventData e)
    {
        if (!bagPanel.activeSelf) return;

        var stack = grid.Inventory[index];
        if (stack.IsEmpty) return;

        sourceGrid = grid;
        sourceIndex = index;
        grid.SetMarked(index);

        Sprite sprite = stack.Item.Icon;
        ghostImage.sprite = sprite;
        ghostImage.color = sprite != null ? Color.white : new Color(1f, 1f, 1f, 0.5f);

        ghost.position = e.position;
        ghost.gameObject.SetActive(true);
        ghost.SetAsLastSibling();
    }

    private void HandleDrag(PointerEventData e)
    {
        if (Dragging) ghost.position = e.position;
    }

    private void HandleDrop(InventoryGridUI target, int index)
    {
        if (!Dragging) return;
        sourceGrid.Inventory.MoveOrSwap(sourceIndex, target.Inventory, index);
    }

    private void CancelDrag()
    {
        if (sourceGrid != null) sourceGrid.SetMarked(-1);
        sourceGrid = null;
        sourceIndex = -1;
        if (ghost != null) ghost.gameObject.SetActive(false);
    }
}
