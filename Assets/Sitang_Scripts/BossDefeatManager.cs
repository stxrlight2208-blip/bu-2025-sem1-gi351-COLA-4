
using UnityEngine;
using UnityEngine.SceneManagement;

public class BossDefeatManager : MonoBehaviour
{
    public string menuSceneName = "MainMenu";

    public void BackToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(menuSceneName);
    }
}
