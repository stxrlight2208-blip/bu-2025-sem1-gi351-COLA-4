using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerDeathSceneTrigger : MonoBehaviour
{
    [Header("ตั้งค่าหน้าจอ Game Over")]
    public float delayBeforeShowUI = 1.0f;

    private bool hasTriggered = false;
    private bool foundPlayerOnce = false;
    private PlayerHealth targetHealth;

    void Update()
    {
        if (hasTriggered) return;

        // 1. ค้นหาตัวละครในฉาก (รองรับทั้ง Player, Player2 และ Player(Clone))
        if (targetHealth == null)
        {
            targetHealth = FindFirstObjectByType<PlayerHealth>();
            if (targetHealth != null)
            {
                foundPlayerOnce = true;
            }
        }

        // 2. เช็กว่าเลือดหมด (<= 0) หรือตัวละครหายไปจากฉากหลังเกิดแล้ว
        if (targetHealth != null)
        {
            if (targetHealth.currentHealth <= 0 || !targetHealth.gameObject.activeInHierarchy)
            {
                TriggerGameOverUI();
            }
        }
        else if (foundPlayerOnce)
        {
            TriggerGameOverUI();
        }
    }

    void TriggerGameOverUI()
    {
        hasTriggered = true;
        StartCoroutine(ShowGameOverRoutine());
    }

    IEnumerator ShowGameOverRoutine()
    {
        yield return new WaitForSecondsRealtime(delayBeforeShowUI);
        BuildGameOverUI();
    }

    void BuildGameOverUI()
    {
        // เช็ก EventSystem เพื่อให้ปุ่มกดคลิกได้แน่นอน
        if (FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<StandaloneInputModule>();
        }

        // สร้าง Canvas แยกอยู่บนสุดของจอ
        GameObject canvasObj = new GameObject("GameOver_Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        // พื้นหลังสีดำโปร่งแสงคลุมทั้งจอ
        GameObject overlay = new GameObject("DarkOverlay");
        overlay.transform.SetParent(canvas.transform, false);
        RectTransform overlayRect = overlay.AddComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;
        Image overlayImg = overlay.AddComponent<Image>();
        overlayImg.color = new Color(0.05f, 0.02f, 0.04f, 0.82f);

        // ข้อความหัวข้อ YOU DIED
        CreateText(overlay.transform, "GameOverTitle", "YOU DIED", 76, new Color(0.92f, 0.16f, 0.22f, 1f), new Vector2(0f, 110f), new Vector2(800f, 120f));

        // ปุ่ม RETRY (รีเซ็ตด่านปัจจุบันใหม่)
        CreateButton(
            overlay.transform,
            "RetryButton",
            "RETRY",
            new Color(0.85f, 0.18f, 0.24f, 1f),
            new Vector2(0f, -20f),
            new Vector2(280f, 64f),
            OnRetryClicked
        );

        // ปุ่ม QUIT (ออกจากเกมทันที)
        CreateButton(
            overlay.transform,
            "QuitButton",
            "QUIT",
            new Color(0.22f, 0.22f, 0.26f, 1f),
            new Vector2(0f, -105f),
            new Vector2(280f, 56f),
            OnQuitClicked
        );
    }

    void OnRetryClicked()
    {
        Time.timeScale = 1f;
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }

    void OnQuitClicked()
    {
        Time.timeScale = 1f;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void CreateText(Transform parent, string name, string content, int fontSize, Color color, Vector2 anchoredPos, Vector2 size)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;

        Text txt = obj.AddComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.text = content;
        txt.fontSize = fontSize;
        txt.fontStyle = FontStyle.Bold;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = color;

        Shadow shadow = obj.AddComponent<Shadow>();
        shadow.effectColor = Color.black;
        shadow.effectDistance = new Vector2(3f, -3f);
    }

    void CreateButton(Transform parent, string name, string label, Color bgColor, Vector2 anchoredPos, Vector2 size, UnityEngine.Events.UnityAction onClickAction)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        RectTransform rect = btnObj.AddComponent<RectTransform>();
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;

        Image img = btnObj.AddComponent<Image>();
        img.color = bgColor;

        Button btn = btnObj.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.highlightedColor = Color.Lerp(bgColor, Color.white, 0.25f);
        colors.pressedColor = Color.Lerp(bgColor, Color.black, 0.25f);
        btn.colors = colors;
        btn.onClick.AddListener(onClickAction);

        CreateText(btnObj.transform, "Label", label, 26, Color.white, Vector2.zero, size);
    }
}