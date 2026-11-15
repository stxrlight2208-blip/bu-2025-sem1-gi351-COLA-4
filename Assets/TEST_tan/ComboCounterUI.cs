using UnityEngine;
using UnityEngine.UI;

public class ComboCounterUI : MonoBehaviour
{
    public static ComboCounterUI instance;

    [Header("Combo Settings")]
    public float comboDuration = 2.2f; // ระยะเวลาต่อคอมโบก่อนรีเซ็ต (วินาที)

    private int currentHits = 0;
    private int totalDamageInCombo = 0;
    private float comboTimer = 0f;

    // UI Elements (สร้างอัตโนมัติที่มุมขวาบน)
    private GameObject hudContainer;
    private RectTransform containerRect;
    private Text hitNumberText;
    private Text rankLabelText;
    private Text totalDamageText;
    private Image timerBarFill;

    // สร้างตัวเองอัตโนมัติเมื่อเริ่มเกม (ไม่ต้องลากใส่ฉากเอง)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoCreate()
    {
        if (instance == null)
        {
            GameObject obj = new GameObject("ComboCounterUI_Auto");
            obj.AddComponent<ComboCounterUI>();
        }
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        BuildTopRightComboUI();
    }

    void BuildTopRightComboUI()
    {
        GameObject canvasObj = new GameObject("ComboHUD_Canvas");
        canvasObj.transform.SetParent(transform);
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 150;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        // กล่องหลักยึดมุมขวาบน (Top-Right Anchor)
        hudContainer = new GameObject("TopRight_ComboBox");
        hudContainer.transform.SetParent(canvasObj.transform, false);
        containerRect = hudContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(1f, 1f);
        containerRect.anchorMax = new Vector2(1f, 1f);
        containerRect.pivot = new Vector2(1f, 1f);
        containerRect.anchoredPosition = new Vector2(-45f, -40f);
        containerRect.sizeDelta = new Vector2(320f, 170f);

        // พื้นหลังโปร่งแสงบางๆ
        Image bg = hudContainer.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.05f, 0.08f, 0.55f);

        // 1. ตัวเลขจำนวน Hit ขนาดใหญ่ (เช่น "5 HITS")
        GameObject hitObj = new GameObject("HitCountText");
        hitObj.transform.SetParent(hudContainer.transform, false);
        RectTransform hitRect = hitObj.AddComponent<RectTransform>();
        hitRect.anchorMin = new Vector2(0f, 0.45f);
        hitRect.anchorMax = new Vector2(1f, 1f);
        hitRect.offsetMin = new Vector2(15f, 0f);
        hitRect.offsetMax = new Vector2(-20f, -8f);

        hitNumberText = hitObj.AddComponent<Text>();
        hitNumberText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        hitNumberText.fontSize = 54;
        hitNumberText.fontStyle = FontStyle.BoldAndItalic;
        hitNumberText.alignment = TextAnchor.MiddleRight;
        hitNumberText.color = Color.white;

        // 2. ป้ายระดับ Rank (เช่น "GREAT!", "SSSTYLISH!!!")
        GameObject rankObj = new GameObject("RankText");
        rankObj.transform.SetParent(hudContainer.transform, false);
        RectTransform rankRect = rankObj.AddComponent<RectTransform>();
        rankRect.anchorMin = new Vector2(0f, 0.22f);
        rankRect.anchorMax = new Vector2(1f, 0.5f);
        rankRect.offsetMin = new Vector2(15f, 0f);
        rankRect.offsetMax = new Vector2(-20f, 0f);

        rankLabelText = rankObj.AddComponent<Text>();
        rankLabelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        rankLabelText.fontSize = 26;
        rankLabelText.fontStyle = FontStyle.Bold;
        rankLabelText.alignment = TextAnchor.MiddleRight;

        // 3. ยอดดาเมจรวมในคอมโบชุดนี้
        GameObject dmgObj = new GameObject("TotalDmgText");
        dmgObj.transform.SetParent(hudContainer.transform, false);
        RectTransform dmgRect = dmgObj.AddComponent<RectTransform>();
        dmgRect.anchorMin = new Vector2(0f, 0.08f);
        dmgRect.anchorMax = new Vector2(1f, 0.26f);
        dmgRect.offsetMin = new Vector2(15f, 0f);
        dmgRect.offsetMax = new Vector2(-20f, 0f);

        totalDamageText = dmgObj.AddComponent<Text>();
        totalDamageText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        totalDamageText.fontSize = 18;
        totalDamageText.alignment = TextAnchor.MiddleRight;
        totalDamageText.color = new Color(0.85f, 0.85f, 0.85f, 1f);

        // 4. หลอดเวลาถอยหลังด้านล่างสุด
        GameObject barBgObj = new GameObject("TimerBarBg");
        barBgObj.transform.SetParent(hudContainer.transform, false);
        RectTransform barBgRect = barBgObj.AddComponent<RectTransform>();
        barBgRect.anchorMin = new Vector2(0f, 0f);
        barBgRect.anchorMax = new Vector2(1f, 0f);
        barBgRect.pivot = new Vector2(0.5f, 0f);
        barBgRect.anchoredPosition = Vector2.zero;
        barBgRect.sizeDelta = new Vector2(0f, 8f);
        Image barBgImg = barBgObj.AddComponent<Image>();
        barBgImg.color = new Color(0.15f, 0.15f, 0.15f, 1f);

        GameObject barFillObj = new GameObject("TimerBarFill");
        barFillObj.transform.SetParent(barBgObj.transform, false);
        RectTransform fillRect = barFillObj.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        timerBarFill = barFillObj.AddComponent<Image>();
        timerBarFill.color = Color.cyan;
        timerBarFill.type = Image.Type.Filled;
        timerBarFill.fillMethod = Image.FillMethod.Horizontal;

        hudContainer.SetActive(false);
    }

    void Update()
    {
        if (currentHits <= 0) return;

        // ระหว่างกำลังกด QTE ให้หยุดนับเวลาถอยหลังชั่วคราว (คอมโบไม่หลุดระหว่างคัตซีน)
        if (!FinisherManager.isQTEActive)
        {
            comboTimer -= Time.deltaTime;
        }

        if (comboTimer <= 0f)
        {
            ResetCombo();
            return;
        }

        // อัปเดตหลอดเวลาและคืนขนาดกล่องให้เด้งสมูท
        timerBarFill.fillAmount = Mathf.Clamp01(comboTimer / comboDuration);
        containerRect.localScale = Vector3.Lerp(containerRect.localScale, Vector3.one, 14f * Time.unscaledDeltaTime);
    }

    // เรียกใช้ฟังก์ชันนี้ทุกครั้งที่โจมตีโดนเป้าหมาย
    public static void RegisterHit(int damageDealt)
    {
        if (instance == null)
        {
            AutoCreate();
        }
        instance.AddHitInternal(damageDealt);
    }

    private void AddHitInternal(int damageDealt)
    {
        currentHits++;
        totalDamageInCombo += damageDealt;
        comboTimer = comboDuration;

        hudContainer.SetActive(true);
        hitNumberText.text = currentHits == 1 ? "1 HIT" : $"{currentHits} HITS";
        totalDamageText.text = $"TOTAL DMG: {totalDamageInCombo}";

        // เปลี่ยนสีและระดับ Rank ตามจำนวนคอมโบ
        Color rankColor;
        string rankTitle;

        if (currentHits >= 10)
        {
            rankColor = new Color(1f, 0.22f, 0.22f, 1f); // สีแดงเพลิง
            rankTitle = "SSSTYLISH!!!";
        }
        else if (currentHits >= 6)
        {
            rankColor = new Color(1f, 0.82f, 0.1f, 1f);  // สีเหลืองทอง
            rankTitle = "BRUTAL!!";
        }
        else if (currentHits >= 3)
        {
            rankColor = new Color(0.25f, 0.9f, 1f, 1f);  // สีฟ้าสว่าง
            rankTitle = "GREAT! (FINISHER READY)";
        }
        else
        {
            rankColor = Color.white;
            rankTitle = "COMBO";
        }

        hitNumberText.color = rankColor;
        rankLabelText.color = rankColor;
        rankLabelText.text = rankTitle;
        timerBarFill.color = rankColor;

        // ขยายเด้งป้ายทุกครั้งที่ฟันโดน
        containerRect.localScale = Vector3.one * 1.22f;
    }

    public void ResetCombo()
    {
        currentHits = 0;
        totalDamageInCombo = 0;
        hudContainer.SetActive(false);
    }
}