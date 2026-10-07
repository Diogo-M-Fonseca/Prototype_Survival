using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private string mainGameSceneName;

    public void StartGame()
    {
        SceneManager.LoadScene(mainGameSceneName);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
