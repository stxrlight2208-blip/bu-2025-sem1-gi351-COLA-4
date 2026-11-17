
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("Game Scene")]
    public string gameSceneName = "GameScene";

    // ปุ่ม PLAY
    public void PlayGame()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    // ปุ่ม QUIT
    public void QuitGame()
    {
        Application.Quit();

        Debug.Log("Quit Game");
    }
}
