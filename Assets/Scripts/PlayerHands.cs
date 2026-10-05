using UnityEngine;

[RequireComponent(typeof(Inventory))]
public class PlayerHands : MonoBehaviour
{
    
    public Inventory Inventory {  get; private set; }

    private void Awake()
    {
        Inventory = GetComponent<Inventory>();
    }
}