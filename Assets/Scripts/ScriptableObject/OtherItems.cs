using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Items/Other Items")]
public class OtherItems : Item
{
    private const int MaxMaterials = 3;

    [SerializeField] private BaseMaterials[] _baseMaterials = new BaseMaterials[1];

    public int BaseMaterialCount => _baseMaterials != null ? _baseMaterials.Length : 0;
    public BaseMaterials[] BaseMaterials => _baseMaterials;

    protected override void OnValidate()
    {
        base.OnValidate();

        if (_baseMaterials != null && _baseMaterials.Length > MaxMaterials)
            System.Array.Resize(ref _baseMaterials, MaxMaterials);
    }
}
