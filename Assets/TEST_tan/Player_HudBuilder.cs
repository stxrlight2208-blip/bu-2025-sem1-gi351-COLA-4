using UnityEngine;
using UnityEngine.UI;

public class PlayerHUDBuilder : MonoBehaviour
{
    [Header("ตำแหน่งและขนาด UI มุมซ้ายบน")]
    public Vector2 hudOffset = new Vector2(30f, -30f);
    public float hpBarWidth = 340f;
    public float hpBarHeight = 28f;

    [Header("สีของหลอด")]
    public Color hpColor = new Color(0.88f, 0.16f, 0.22f, 1f);
    public Color damageLagColor = new Color(1f, 0.85f, 0.35f, 1f);

    private PlayerHealth targetHealth;

    private RectTransform hpFillRect;
    private RectTransform hpLagRect;
    private Text hpText;

    private float lagRatio = 1f;

    void Start()
    {
        BuildHUDCanvas();
    }

    void BuildHUDCanvas()
    {
        GameObject canvasObj = new GameObject("PlayerHUD_Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        // กล่องหลักมุมซ้ายบน
        GameObject root = new GameObject("PlayerStatusHUD");
        root.transform.SetParent(canvas.transform, false);
        RectTransform rootRect = root.AddComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0f, 1f);
        rootRect.anchorMax = new Vector2(0f, 1f);
        rootRect.pivot = new Vector2(0f, 1f);
        rootRect.anchoredPosition = hudOffset;
        rootRect.sizeDelta = new Vector2(hpBarWidth, hpBarHeight);

        // 1. พื้นหลังหลอดเลือด
        GameObject hpBg = CreatePanel(root.transform, "HP_Background", Vector2.zero, new Vector2(hpBarWidth, hpBarHeight), new Color(0.08f, 0.08f, 0.1f, 0.92f));

        // 2. หลอดสีเหลือง (ลดตามหลัง) และหลอดสีแดง (เลือดปัจจุบัน)
        hpLagRect = CreateStretchBar(hpBg.transform, "HP_DamageLag", damageLagColor);
        hpFillRect = CreateStretchBar(hpBg.transform, "HP_Fill", hpColor);

        // 3. ตัวเลข HP
        hpText = CreateLabel(hpBg.transform, "HP_Text", "HP  100 / 100", 16, TextAnchor.MiddleLeft, new Vector2(12f, 0f), new Vector2(hpBarWidth - 20f, hpBarHeight));
    }

    void Update()
    {
        if (targetHealth == null)
            targetHealth = FindFirstObjectByType<PlayerHealth>();

        if (targetHealth != null && hpFillRect != null)
        {
            float maxHp = Mathf.Max(1f, targetHealth.maxHealth);
            float curHp = Mathf.Clamp(targetHealth.currentHealth, 0f, maxHp);
            float targetRatio = curHp / maxHp;

            hpFillRect.anchorMax = new Vector2(targetRatio, 1f);

            if (lagRatio > targetRatio)
                lagRatio = Mathf.MoveTowards(lagRatio, targetRatio, Time.deltaTime * 0.5f);
            else
                lagRatio = targetRatio;

            hpLagRect.anchorMax = new Vector2(lagRatio, 1f);
            hpText.text = $"HP  {Mathf.CeilToInt(curHp)} / {Mathf.CeilToInt(maxHp)}";
        }
    }

    GameObject CreatePanel(Transform parent, string name, Vector2 anchoredPos, Vector2 size, Color bgColor)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;

        Image img = obj.AddComponent<Image>();
        img.color = bgColor;
        return obj;
    }

    RectTransform CreateStretchBar(Transform parent, string name, Color fillColor)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.offsetMin = new Vector2(3f, 3f);
        rect.offsetMax = new Vector2(-3f, -3f);

        Image img = obj.AddComponent<Image>();
        img.color = fillColor;
        return rect;
    }

    Text CreateLabel(Transform parent, string name, string content, int fontSize, TextAnchor align, Vector2 anchoredPos, Vector2 size)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;

        Text txt = obj.AddComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.text = content;
        txt.fontSize = fontSize;
        txt.fontStyle = FontStyle.Bold;
        txt.alignment = align;
        txt.color = Color.white;

        Shadow shadow = obj.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.9f);
        shadow.effectDistance = new Vector2(1.5f, -1.5f);
        return txt;
    }
}