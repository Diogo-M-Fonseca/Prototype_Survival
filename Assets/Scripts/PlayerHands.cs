using UnityEngine;

[RequireComponent(typeof(Inventory))]
public class PlayerHands : MonoBehaviour
{
    public static PlayerHands Instance { get; private set; }

    private Inventory inventory;
    public Inventory Inventory => inventory;

    private void Awake()
    {
        Instance = this;
        inventory = GetComponent<Inventory>();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}