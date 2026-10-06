using UnityEngine;

[RequireComponent(typeof(Inventory))]
public class StorageContainer : Interactable
{
    [SerializeField] private string _title = "Storage Container";

    [SerializeField, Min(0.5f)] private float _closeDistance = 4f;

    private Inventory _inventory;

    public Inventory Inventory => _inventory != null ? _inventory : (_inventory = GetComponent<Inventory>());

    public string Title => _title;

    public float CloseDistance => _closeDistance;

    protected override string DefaultPrompt => "open";

    protected override void OnInteract(GameObject interactor)
    {
        StorageUI ui = FindFirstObjectByType<StorageUI>();

        if (ui == null)
        {
            Debug.LogError($"{name}: não existe nenhum StorageUI ativo na cena.", this);
            return;
        }

        ui.Open(this, interactor.transform);
    }

}
