using UnityEngine;

public abstract class Item : ScriptableObject
{
    [SerializeField] private string displayName;
    [SerializeField, TextArea] private string description;
    [SerializeField] private Sprite icon;
    [SerializeField, Min(1)] private int maxStack = 1;
    [SerializeField] private GameObject heldPrefab;
    [SerializeField] private bool hasDurability;
    [SerializeField, Min(1)] private int maxDurability = 100;

    public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
    public string Description => description;
    public Sprite Icon => icon;
    public int MaxStack => hasDurability ? 1 : maxStack;
    public GameObject HeldPrefab => heldPrefab;
    public bool HasDurability => hasDurability;
    public int MaxDurability => maxDurability;
    public virtual bool Use(GameObject user) => false;

    private void OnValidate()
    {
        if (hasDurability) maxStack = 1;
    }
}
