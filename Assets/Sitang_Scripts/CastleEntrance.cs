
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class CastleEntrance : MonoBehaviour
{
    public GameObject enterPrompt;
    public string bossSceneName = "BossScene";

    private bool playerNear = false;

    void Start()
    {
        enterPrompt.SetActive(false);
    }

    void Update()
    {
        if (playerNear && Input.GetKeyDown(KeyCode.E))
        {
            SceneManager.LoadScene(bossSceneName);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerNear = true;
            enterPrompt.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerNear = false;
            enterPrompt.SetActive(false);
        }
    }
}
