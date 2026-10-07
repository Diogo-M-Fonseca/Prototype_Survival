using UnityEngine;

[CreateAssetMenu(fileName = "New Tool", menuName = "Items/Tool")]

public class Tools : Item
{
    [SerializeField] private ToolTypes _toolType;

    public ToolTypes ToolType => _toolType;
}
