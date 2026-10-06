using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [SerializeField] private int _maxHealth = 100;
    [SerializeField] private int _currentHealth;
    [SerializeField] private int _currentHunger = 100;
    [SerializeField] private int _currentThirst = 100;

    [Tooltip("Pontos de fome perdidos por segundo.")]
    [SerializeField] private float _hungerDecreaseRate = 2f;
    [Tooltip("Pontos de sede perdidos por segundo.")]
    [SerializeField] private float _thirstDecreaseRate = 2.5f;
    [SerializeField] private float _starvationDamageRate = 3f;

    private float _hungerDrain;
    private float _thirstDrain;
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
        _currentHunger -= Drain(ref _hungerDrain, _hungerDecreaseRate);
        _currentThirst -= Drain(ref _thirstDrain, _thirstDecreaseRate);

        _currentHunger = Mathf.Clamp(_currentHunger, 0, 100);
        _currentThirst = Mathf.Clamp(_currentThirst, 0, 100);

        if (_currentHunger <= 0 || _currentThirst <= 0)
        {
            _starvationTimer += Time.deltaTime;
            if (_starvationTimer >= 1f)
            {
                TakeDamage(1);
                _starvationTimer = 0f;
            }
        }
        else
        {
            _starvationTimer = 0f;
        }
    }

    private static int Drain(ref float accumulator, float ratePerSecond)
    {
        accumulator += ratePerSecond * Time.deltaTime;
        int whole = Mathf.FloorToInt(accumulator);
        accumulator -= whole;
        return whole;
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

        _currentThirst = Mathf.Min(_currentThirst + thirstAmount, 100);
        return true;
    }
}
