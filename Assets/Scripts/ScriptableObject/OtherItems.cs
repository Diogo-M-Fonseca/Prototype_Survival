using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Items/Other Items")]
public class OtherItems : Item
{
    [SerializeField, Range(1, 3)] private int baseMaterialCount = 1;
    [SerializeField] private BaseMaterials[] baseMaterials = new BaseMaterials[1];

    public int BaseMaterialCount => baseMaterialCount;
    public BaseMaterials[] BaseMaterials => baseMaterials;

    private void OnValidate()
    {
        baseMaterialCount = Mathf.Clamp(baseMaterialCount, 1, 3);

        if (baseMaterials == null || baseMaterials.Length != baseMaterialCount)
        {
            BaseMaterials[] resized = new BaseMaterials[baseMaterialCount];
            if (baseMaterials != null)
                System.Array.Copy(baseMaterials, resized, Mathf.Min(baseMaterials.Length, resized.Length));

            baseMaterials = resized;
        }
    }
}
