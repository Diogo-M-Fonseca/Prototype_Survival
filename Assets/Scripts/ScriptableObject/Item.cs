using UnityEngine;

public abstract class Item : ScriptableObject
{
    /// <summary>
    /// Nome mostrado ao jogador (se vazio usa o nome do asset).
    /// </summary>
    [SerializeField] private string _displayName;

    /// <summary>
    /// Descrição do item.
    /// </summary>
    [SerializeField, TextArea] private string _description;

    /// <summary>
    /// Ícone manual, usado se não for possível gerar um a partir do prefab.
    /// </summary>
    [SerializeField] private Sprite _icon;

    /// <summary>
    /// Máximo de unidades por stack.
    /// </summary>
    [SerializeField, Min(1)] private int _maxStack = 1;

    /// <summary>
    /// Modelo 3D mostrado na mão (e usado para gerar o ícone).
    /// </summary>
    [SerializeField] private GameObject _heldPrefab;

    /// <summary>
    /// Se o item se desgasta com o tempo.
    /// </summary>
    [SerializeField] private bool _hasDurability;

    /// <summary>
    /// Durabilidade máxima (só usada se tiver durabilidade).
    /// </summary>
    [SerializeField, Min(1)] private int _maxDurability = 100;

    /// <summary>
    /// Item que fica no lugar deste quando uma unidade é gasta.
    /// </summary>
    [SerializeField] private Item _leftoverItem;

    /// <summary>
    /// Etiquetas do item. Os alvos do mundo (ItemTarget) usam-nas para decidir que itens aceitam.
    /// </summary>
    [SerializeField] private ItemTag[] _tags;

    /// <summary>
    /// Som tocado quando o item é usado com sucesso (comer, beber, aplicar ligadura...).
    /// </summary>
    [SerializeField] private AudioClip _useSound;

    /// <summary>
    /// Volume do som de uso.
    /// </summary>
    [SerializeField, Range(0f, 1f)] private float _useSoundVolume = 1f;

    /// <summary>
    /// Ícone já resolvido (cache, para não o gerar de novo todas as vezes).
    /// </summary>
    private Sprite _resolvedIcon;

    /// <summary>
    /// Nome a mostrar: o personalizado ou o nome do asset.
    /// </summary>
    public string DisplayName => string.IsNullOrEmpty(_displayName) ? name : _displayName;

    /// <summary>
    /// Descrição do item.
    /// </summary>
    public string Description => _description;

    /// <summary>
    /// Ícone do item: gerado a partir do prefab, se existir; caso contrário o ícone manual.
    /// </summary>
    public Sprite Icon
    {
        get
        {
            if (_resolvedIcon == null)
            {
                Sprite generated = _heldPrefab != null ? ItemIconRenderer.Get(_heldPrefab) : null;
                _resolvedIcon = generated != null ? generated : _icon;
            }
            return _resolvedIcon;
        }
    }

    /// <summary>
    /// Stack máximo (sempre 1 para itens com durabilidade).
    /// </summary>
    public int MaxStack => _hasDurability ? 1 : _maxStack;

    /// <summary>
    /// Modelo 3D mostrado na mão.
    /// </summary>
    public GameObject HeldPrefab => _heldPrefab;

    /// <summary>
    /// True se o item tem durabilidade.
    /// </summary>
    public bool HasDurability => _hasDurability;

    /// <summary>
    /// Durabilidade máxima.
    /// </summary>
    public int MaxDurability => _maxDurability;

    /// <summary>
    /// Item deixado para trás ao gastar uma unidade (null se não deixar nada).
    /// </summary>
    public Item LeftoverItem => _leftoverItem;

    /// <summary>
    /// Indica se o item tem uma determinada etiqueta.
    /// </summary>
    /// <param name="tag">Etiqueta a procurar.</param>
    public bool HasTag(ItemTag tag)
    {
        if (tag == null || _tags == null) return false;

        foreach (ItemTag t in _tags)
            if (t == tag) return true;

        return false;
    }

    /// <summary>
    /// Toca o som de uso do item (se tiver) na posição de quem o usou.
    /// </summary>
    /// <param name="user">Quem usou o item.</param>
    public void PlayUseSound(GameObject user)
    {
        if (_useSound == null || user == null) return;
        AudioSource.PlayClipAtPoint(_useSound, user.transform.position, _useSoundVolume);
    }

    /// <summary>
    /// Usa o item. Por omissão não faz nada; as subclasses sobrepõem.
    /// </summary>
    /// <param name="user">Quem usa o item.</param>
    /// <returns>true se o uso consumiu o item.</returns>
    public virtual bool Use(GameObject user) => false;

    /// <summary>
    /// No editor: força stack 1 para itens com durabilidade e invalida o ícone em cache.
    /// </summary>
    protected virtual void OnValidate()
    {
        if (_hasDurability) _maxStack = 1;
        _resolvedIcon = null;
    }
}