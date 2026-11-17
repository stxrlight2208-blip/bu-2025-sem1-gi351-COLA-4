using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StyleRankManager : MonoBehaviour
{
    public static StyleRankManager Instance;

    [Header("UI References")]
    public GameObject rankContainer; 
    public TextMeshProUGUI rankText;
    public Slider decaySlider;

    [Header("Style Settings")]
    public float maxPointsPerRank = 100f; 
    public float decayRate = 25f;         
    
    [Header("Punch Scale Settings")]
    public float normalPunchScale = 1.08f; 
    public float rankUpPunchScale = 1.25f; 
    public float scaleReturnSpeed = 8f;  // 🟢 ปรับลดลงนิดหน่อยจาก 12 เป็น 8 จะทำให้จังหวะยืดหดนุ่มนวลขึ้น ไม่กระชาก

    private float currentPoints = 0f;
    private int currentRankIndex = -1; 

    private readonly string[] rankNames = { "D", "C", "B", "A", "S", "SS", "SSS" };
    private readonly Color[] rankColors = new Color[]
    {
        new Color(0.6f, 0.6f, 0.6f), 
        new Color(0.2f, 0.6f, 1.0f), 
        new Color(0.2f, 0.9f, 0.4f), 
        new Color(1.0f, 0.8f, 0.1f), 
        new Color(1.0f, 0.4f, 0.0f), 
        new Color(1.0f, 0.1f, 0.1f), 
        new Color(0.9f, 0.1f, 1.0f)  
    };

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (rankContainer != null)
        {
            rankContainer.SetActive(false);
        }
    }

    void Update()
    {
        // ควบคุมขนาดตัวหนังสือให้สู่อัตราปกติอย่างสมูทตลอดเวลา
        if (rankText != null)
        {
            rankText.transform.localScale = Vector3.Lerp(
                rankText.transform.localScale,
                Vector3.one,
                Time.deltaTime * scaleReturnSpeed
            );
        }

        if (currentRankIndex < 0) return;

        currentPoints -= decayRate * Time.deltaTime;

        if (currentPoints <= 0f)
        {
            int previousRank = currentRankIndex;
            currentRankIndex--;
            
            if (currentRankIndex < 0)
            {
                currentPoints = 0f;
                if (rankContainer != null) rankContainer.SetActive(false);
                return;
            }
            else
            {
                currentPoints = maxPointsPerRank;
                
                // 🟢 เพิ่มเอฟเฟกต์เบาๆ ตอนแรงก์ตก เพื่อให้มันสมูท ไม่แข็งโป๊ก
                UpdateRankVisualsSmooth();
            }
        }

        if (decaySlider != null)
        {
            decaySlider.value = currentPoints / maxPointsPerRank;
        }
    }

    public void AddStylePoints(float points)
    {
        if (currentRankIndex < 0)
        {
            currentRankIndex = 0; 
            currentPoints = points;
            if (rankContainer != null) rankContainer.SetActive(true);
            UpdateRankVisuals(true);
            return;
        }

        currentPoints += points;

        bool hasRankUp = false;
        while (currentPoints >= maxPointsPerRank && currentRankIndex < rankNames.Length - 1)
        {
            currentPoints -= maxPointsPerRank;
            currentRankIndex++;
            hasRankUp = true; 
        }

        if (currentRankIndex == rankNames.Length - 1 && currentPoints > maxPointsPerRank)
        {
            currentPoints = maxPointsPerRank;
        }

        float targetScale = hasRankUp ? rankUpPunchScale : normalPunchScale;
        UpdateRankVisualsCustom(targetScale);
    }

    public void OnPlayerHit()
    {
        if (currentRankIndex >= 0)
        {
            currentPoints -= 10f; 
            if (currentPoints < 0f)
            {
                currentPoints = 5f; 
            }
            
            if (rankText != null)
            {
                rankText.text = rankNames[currentRankIndex];
                rankText.color = rankColors[currentRankIndex];
            }
        }
    }

    void UpdateRankVisuals(bool punchEffect)
    {
        if (rankText == null || currentRankIndex < 0) return;

        rankText.text = rankNames[currentRankIndex];
        rankText.color = rankColors[currentRankIndex];

        if (punchEffect)
        {
            rankText.transform.localScale = Vector3.one * normalPunchScale;
        }
    }

    void UpdateRankVisualsCustom(float customScale)
    {
        if (rankText == null || currentRankIndex < 0) return;

        rankText.text = rankNames[currentRankIndex];
        rankText.color = rankColors[currentRankIndex];
        rankText.transform.localScale = Vector3.one * customScale;
    }

    // 🟢 ฟังก์ชันใหม่: ช่วยอัปเดตแรงก์ตอนตกลงมาแบบนุ่มนวลโดยให้ขนาดขยายขึ้นเล็กน้อยก่อนจะสอดรับกับการ Lerp กลับมาปกติ
    void UpdateRankVisualsSmooth()
    {
        if (rankText == null || currentRankIndex < 0) return;

        rankText.text = rankNames[currentRankIndex];
        rankText.color = rankColors[currentRankIndex];
        rankText.transform.localScale = Vector3.one * 1.15f; // ให้เด้งตัวเบาๆ ตอนตกแรงก์ จะได้ดูสมูทเป็นธรรมชาติ
    }
}