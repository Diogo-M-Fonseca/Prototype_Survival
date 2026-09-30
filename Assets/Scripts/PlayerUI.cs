using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
    [SerializeField] private PlayerStats _playerStats;
    [SerializeField] private Slider _healthBar;
    [SerializeField] private Slider _hungerBar;
    [SerializeField] private Slider _thirstBar;

    private void Update()
    {
        UpdateUI();
    }

    private void UpdateUI()
    {
        _healthBar.value = (float)_playerStats.CurrentHealth / _playerStats.MaxHealth;
        _hungerBar.value = (float)_playerStats.CurrentHunger / 100f;
        _thirstBar.value = (float)_playerStats.CurrentThirst / 100f;
    }
}
