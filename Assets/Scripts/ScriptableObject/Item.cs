using UnityEngine;

public abstract class Item : ScriptableObject
{
    [SerializeField] private string displayName;
    [SerializeField, TextArea] private string description;
    [SerializeField] private Sprite icon;
    [SerializeField, Min(1)] private int maxStack = 1;
    [SerializeField] private GameObject heldPrefab;

    public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
    public string Description => description;
    public Sprite Icon => icon;
    public int MaxStack => maxStack;
    public GameObject HeldPrefab => heldPrefab;

    public virtual bool Use(GameObject user) => false;
}
