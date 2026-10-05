using UnityEngine;

public abstract class Item : ScriptableObject
{
    [SerializeField] private string _displayName;
    [SerializeField, TextArea] private string _description;
    [SerializeField] private Sprite _icon;
    [SerializeField, Min(1)] private int _maxStack = 1;
    [SerializeField] private GameObject _heldPrefab;
    [SerializeField] private bool _hasDurability;
    [SerializeField, Min(1)] private int _maxDurability = 100;

    public string DisplayName => string.IsNullOrEmpty(_displayName) ? name : _displayName;

    public string Description => _description;

    public Sprite Icon
    {
        get
        {
            Sprite generated = _heldPrefab != null ? ItemIconRenderer.Get(_heldPrefab) : null;
            return generated != null ? generated : _icon;
        }
    }

    public int MaxStack => _hasDurability ? 1 : _maxStack;

    public GameObject HeldPrefab => _heldPrefab;

    public bool HasDurability => _hasDurability;

    public int MaxDurability => _maxDurability;

    public virtual bool Use(GameObject user) => false;

    private void OnValidate()
    {
        if (_hasDurability) _maxStack = 1;
    }
}
