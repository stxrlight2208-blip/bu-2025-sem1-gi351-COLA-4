using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class CastleEntrance : MonoBehaviour
{
    public GameObject enterPrompt;
    public SceneFade sceneFade;
    public string bossSceneName = "BossScene";

    private bool playerNear = false;
    private bool isTransitioning = false;

    void Start()
    {
        if (enterPrompt != null)
            enterPrompt.SetActive(false);
    }

    void Update()
    {
        if (isTransitioning) return;

        if (playerNear && Input.GetKeyDown(KeyCode.E))
        {
            isTransitioning = true;

            if (enterPrompt != null)
                enterPrompt.SetActive(false);

            if (sceneFade != null)
                sceneFade.FadeToScene(bossSceneName);
            else
                SceneManager.LoadScene(bossSceneName);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isTransitioning) return;

        if (other.CompareTag("Player"))
        {
            playerNear = true;
            if (enterPrompt != null)
                enterPrompt.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (isTransitioning) return;

        if (other.CompareTag("Player"))
        {
            playerNear = false;
            if (enterPrompt != null)
                enterPrompt.SetActive(false);
        }
    }
}