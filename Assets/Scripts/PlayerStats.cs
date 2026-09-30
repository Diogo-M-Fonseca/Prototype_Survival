using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    // Player stats
    [SerializeField] private int _maxHealth = 100;
    [SerializeField] private int _currentHealth;
    [SerializeField] private int _currentHunger = 100;
    [SerializeField] private int _currentThirst = 100;

    public int MaxHealth => _maxHealth;
    public int CurrentHealth => _currentHealth;
    public int CurrentHunger => _currentHunger;
    public int CurrentThirst => _currentThirst;

    private void Awake()
    {
        _currentHealth = _maxHealth;
    }
}
