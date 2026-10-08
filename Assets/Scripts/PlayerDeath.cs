using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(PlayerStats))]
public class PlayerDeath : MonoBehaviour
{
    [SerializeField] private string _mainMenuScene = "MainMenu";

    [SerializeField, Min(0f)] private float _delay = 1.5f;

    [SerializeField] private MonoBehaviour[] _disableOnDeath;

    private PlayerStats _stats;
    private bool _dead;
    private float _loadTime;

    /// <summary>
    /// True depois de o jogador morrer.
    /// </summary>
    public bool IsDead => _dead;

    private void Awake()
    {
        _stats = GetComponent<PlayerStats>();
    }

    private void Update()
    {
        if (!_dead)
        {
            if (_stats.CurrentHealth <= 0) Die();
            return;
        }

        // unscaled: continua a contar mesmo que o timeScale esteja a 0
        if (Time.unscaledTime >= _loadTime) LoadMenu();
    }

    private void Die()
    {
        _dead = true;
        _loadTime = Time.unscaledTime + _delay;

        if (_disableOnDeath != null)
        {
            foreach (MonoBehaviour b in _disableOnDeath)
                if (b != null) b.enabled = false;
        }
    }

    private void LoadMenu()
    {
        enabled = false; // só carrega uma vez

        if (!Application.CanStreamedLevelBeLoaded(_mainMenuScene))
        {
            Debug.LogError($"PlayerDeath: a cena '{_mainMenuScene}' não está nas Build Settings (ou o nome está errado).", this);
            return;
        }

        // O menu precisa do rato livre e do tempo a correr
        Time.timeScale = 1f;
        AudioListener.pause = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SceneManager.LoadScene(_mainMenuScene);
    }
}
