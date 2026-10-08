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
    [SerializeField] private GameObject _gameOverMenu;
    [SerializeField] private Behaviour[] _playerControlScripts;

    private float _hungerDrain;
    private float _thirstDrain;
    private float _starvationTimer;
    private bool _hasDied;

    public int MaxHealth => _maxHealth;
    public int CurrentHealth => _currentHealth;
    public int CurrentHunger => _currentHunger;
    public int CurrentThirst => _currentThirst;

    private void Awake()
    {
        _currentHealth = _maxHealth;

        if (_gameOverMenu == null)
        {
            _gameOverMenu = GameObject.Find("GameOverMenu");
        }

        if (_gameOverMenu != null)
        {
            _gameOverMenu.SetActive(false);
        }
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

        if (!_hasDied && _currentHealth <= 0)
        {
            Death();
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

    private void Death()
    {
        _hasDied = true;

        if (_gameOverMenu == null)
        {
            Debug.LogError("Game Over Menu is not assigned on PlayerStats.", this);
        }
        else
        {
            _gameOverMenu.SetActive(true);
        }

        Behaviour[] controlScripts = _playerControlScripts;
        if (controlScripts == null || controlScripts.Length == 0)
        {
            controlScripts = GetComponents<Behaviour>();
        }

        foreach (Behaviour playerControlScript in controlScripts)
        {
            if (playerControlScript != null && playerControlScript != this)
            {
                playerControlScript.enabled = false;
            }
        }

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
