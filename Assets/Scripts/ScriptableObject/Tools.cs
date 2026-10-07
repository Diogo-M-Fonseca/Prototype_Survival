using UnityEngine;

[CreateAssetMenu(fileName = "New Tool", menuName = "Items/Tool")]

public class Tools : Item
{
    [SerializeField] private ToolTypes _toolType;
    [SerializeField] private bool _canAttack;
    [SerializeField] private int _attackPower;

    public ToolTypes ToolType => _toolType;
    public bool CanAttack => _canAttack;
    public int AttackPower => _attackPower;
}
