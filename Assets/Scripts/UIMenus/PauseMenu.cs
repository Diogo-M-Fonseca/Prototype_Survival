using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject pauseMenuUI;
    [SerializeField] private InputActionReference pauseAction;
    
    public void ContinueGame()
    {
        Time.timeScale = 1f;
        pauseMenuUI.SetActive(false);
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void LoadMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    private void TogglePauseMenu()
    {
        bool isPaused = !pauseMenuUI.activeSelf;

        pauseMenuUI.SetActive(isPaused);
        Time.timeScale = isPaused ? 0f : 1f;
    }

    private void OnEnable()
    {
        pauseAction.action.performed += OnPause;
        pauseAction.action.Enable();
        
    }

    private void OnDisable()
    {
        pauseAction.action.performed -= OnPause;
        pauseAction.action.Disable();
    }

    private void OnPause(InputAction.CallbackContext context)
    {
        TogglePauseMenu();
    }
}
