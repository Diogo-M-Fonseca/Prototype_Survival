using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    // Player stats
    [SerializeField] private int _maxHealth = 100;
    [SerializeField] private int _currentHealth;
    [SerializeField] private int _currentHunger = 100;
    [SerializeField] private int _currentThirst = 100;
    [SerializeField] private float _hungerDecreaseRate = 2f;
    [SerializeField] private float _thirstDecreaseRate = 2.5f;
    [SerializeField] private float _starvationDamageRate = 3f;

    private float _hungerTimer;
    private float _thirstTimer;
    private float _starvationTimer;

    public int MaxHealth => _maxHealth;
    public int CurrentHealth => _currentHealth;
    public int CurrentHunger => _currentHunger;
    public int CurrentThirst => _currentThirst;

    private void Awake()
    {
        _currentHealth = _maxHealth;
    }

    private void Update()
    {
        // Decrease hunger and thirst over time
        _hungerTimer += Time.deltaTime;
        _thirstTimer += Time.deltaTime;

        if (_hungerTimer >= 1f)
        {
            _currentHunger -= Mathf.RoundToInt(_hungerDecreaseRate * Time.deltaTime * 100); // Multiply by 100 to convert to integer
            _hungerTimer = 0f;
        }

        if (_thirstTimer >= 1f)
        {
            _currentThirst -= Mathf.RoundToInt(_thirstDecreaseRate * Time.deltaTime * 100); // Multiply by 100 to convert to integer
            _thirstTimer = 0f;
        }

        // Clamp hunger and thirst values
        _currentHunger = Mathf.Clamp(_currentHunger, 0, 100);
        _currentThirst = Mathf.Clamp(_currentThirst, 0, 100);

        // Check for player death due to hunger or thirst
        if (_currentHunger <= 0 || _currentThirst <= 0)
        {
            _starvationTimer += Time.deltaTime;
            if (_starvationTimer >= 1f)
            {
                TakeDamage(1); // Take damage when hunger or thirst reaches zero
                _starvationTimer = 0f;
            }
        }
    }

    private void TakeDamage(int damage)
    {
        _currentHealth -= damage;
        if (_currentHealth <= 0)
        {
            _currentHealth = 0;
        }
    }

    public bool RestoreHealth(int healthAmount)
    {
        if (healthAmount <= 0 || _currentHealth >= _maxHealth) return false;

        _currentHealth = Mathf.Min(_currentHealth + healthAmount, _maxHealth);
        return true;
    }

    public bool RestoreHunger(int hungerAmount)
    {
        if (hungerAmount <= 0 || _currentHunger >= 100) return false;

        _currentHunger = Mathf.Min(_currentHunger + hungerAmount, 100);
        return true;
    }

    public bool RestoreThirst(int thirstAmount)
    {
        if (thirstAmount <= 0 || _currentThirst >= 100) return false;

        _currentThirst += thirstAmount;
        _currentThirst = Mathf.Min(_currentThirst, 100);
        return true;
    }
}
