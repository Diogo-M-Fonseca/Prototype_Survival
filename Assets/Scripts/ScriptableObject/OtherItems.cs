using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Items/Other Items")]
public class OtherItems : Item
{
    [SerializeField, Range(1, 3)] private int _baseMaterialCount = 1;
    [SerializeField] private BaseMaterials[] _baseMaterials = new BaseMaterials[1];

    public int BaseMaterialCount => _baseMaterialCount;
    public BaseMaterials[] BaseMaterials => _baseMaterials;

    private void OnValidate()
    {
        _baseMaterialCount = Mathf.Clamp(_baseMaterialCount, 1, 3);

        if (_baseMaterials == null || _baseMaterials.Length != _baseMaterialCount)
        {
            BaseMaterials[] resized = new BaseMaterials[_baseMaterialCount];
            if (_baseMaterials != null)
                System.Array.Copy(_baseMaterials, resized, Mathf.Min(_baseMaterials.Length, resized.Length));

            _baseMaterials = resized;
        }
    }
}
