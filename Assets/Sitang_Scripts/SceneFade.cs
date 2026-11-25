
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneFade : MonoBehaviour
{
    public CanvasGroup fadeGroup;
    public float fadeDuration = 1.5f;

    public void FadeToScene(string sceneName)
    {
        StartCoroutine(FadeRoutine(sceneName));
    }

    IEnumerator FadeRoutine(string sceneName)
    {
        float time = 0f;

        fadeGroup.blocksRaycasts = true;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            fadeGroup.alpha = Mathf.Clamp01(time / fadeDuration);
            yield return null;
        }

        fadeGroup.alpha = 1f;
        SceneManager.LoadScene(sceneName);
    }
}
