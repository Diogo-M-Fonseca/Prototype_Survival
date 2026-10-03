using UnityEngine;

[RequireComponent(typeof(Inventory))]
public class PlayerHands : MonoBehaviour
{
    public Inventory Inventory => GetComponent<Inventory>();
}