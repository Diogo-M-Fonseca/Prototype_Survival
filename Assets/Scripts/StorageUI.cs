using TMPro;
using UnityEngine;

public class StorageUI : MonoBehaviour
{
    [SerializeField] private InventoryToggle _bagToggle;
    [SerializeField] private GameObject _panel;
    [SerializeField] private InventoryGridUI _grid;
    [SerializeField] private TMP_Text _title;

    private StorageContainer _current;
    private Transform _interactor;

    private bool IsOpen => _panel.activeSelf;

    private void Awake()
    {
        if (_panel == gameObject)
        {
            Debug.LogError("StorageUI não pode estar no próprio painel. Move-o para o Canvas.", this);
            return;
        }
        _panel.SetActive(false);
    }

    private void OnEnable() => _bagToggle.Toggled += OnBagToggled;
    private void OnDisable() => _bagToggle.Toggled -= OnBagToggled;

    public void Open(StorageContainer container, Transform interactor)
    {
        if (container == null) return;

        _current = container;
        _interactor = interactor;

        _panel.SetActive(true);
        _grid.Bind(container.Inventory);
        if (_title != null) _title.text = container.Title;

        _bagToggle.Open();
    }

    public void Close() => _bagToggle.Close();

    private void OnBagToggled(bool open)
    {
        if (!open) Hide();
    }

    private void Hide()
    {
        _current = null;
        _interactor = null;
        _grid.Unbind();
        _panel.SetActive(false);
    }

    private void Update()
    {
        if (!IsOpen) return;

        bool gone = _current == null || _interactor == null;
        if (gone || Vector3.Distance(_interactor.position, _current.transform.position) > _current.CloseDistance)
            Close();
    }
}
