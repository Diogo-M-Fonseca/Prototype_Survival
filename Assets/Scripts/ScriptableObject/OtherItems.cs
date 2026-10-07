using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Items/Other Items")]
public class OtherItems : Item, IDestroyable
{
    private const int MaxMaterials = 3;

    [SerializeField] private BaseMaterials[] _baseMaterials = new BaseMaterials[1];
    [SerializeField] private int _durability = 1;
    [SerializeField] private GameObject _gameObject;

    public int BaseMaterialCount => _baseMaterials != null ? _baseMaterials.Length : 0;
    public BaseMaterials[] BaseMaterials => _baseMaterials;

    protected override void OnValidate()
    {
        base.OnValidate();

        if (_baseMaterials != null && _baseMaterials.Length > MaxMaterials)
            System.Array.Resize(ref _baseMaterials, MaxMaterials);
    }

    public void TakeDamage(int damage)
    {
        _durability -= damage;
        if (_durability <= 0)
        {
            DestroyObject(_gameObject);
        }
    }

    private void DestroyObject(GameObject gameObject)
    {
        // Spawn base materials code here
        Debug.Log("OtherItem destroyed");
        Destroy(gameObject);
    }
}
