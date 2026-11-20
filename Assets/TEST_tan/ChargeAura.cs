using System.Collections.Generic;
using UnityEngine;

public class ChargeAuraVFX : MonoBehaviour
{
    [Header("Charge Timing (ตั้งค่าให้ตรงกับ PlayerAttack)")]
    public float holdThreshold = 0.2f;   // ระยะเวลาเริ่มแสดงวงชาร์จ
    public float maxChargeTime = 1.0f;   // ระยะเวลาชาร์จเต็มหลอด

    [Header("Dragon Ball Aura Colors")]
    public Color startChargeColor = new Color(0.2f, 0.85f, 1f, 0.9f); // สีฟ้า Ki เริ่มต้น
    public Color fullChargeColor = new Color(1f, 0.85f, 0.1f, 1f);    // สีทอง Super Saiyan ตอนชาร์จเต็ม
    public Color lightningColor = new Color(0.9f, 0.98f, 1f, 1f);     // สีสายฟ้า

    [Header("Layer Settings")]
    public string sortingLayer = "New Layer 3";
    public int baseSortingOrder = 60;

    private float holdTimer = 0f;
    private bool isCharging = false;

    private LineRenderer outerRing;
    private LineRenderer innerRing;
    private List<LineRenderer> auraFlames = new List<LineRenderer>();
    private LineRenderer lightningLine;
    private float lightningTimer = 0f;

    private const int RING_SEGMENTS = 48;
    private const int FLAME_COUNT = 12;

    void Awake()
    {
        BuildAuraComponents();
        SetAuraActive(false);
    }

    void BuildAuraComponents()
    {
        Material additiveMat = new Material(Shader.Find("Sprites/Default"));

        // 1. วงแหวนพลังงานชั้นนอก (หดเข้าหาตัวตอนชาร์จ)
        outerRing = CreateLine("Charge_OuterRing", additiveMat, RING_SEGMENTS + 1, 0.12f, 0.12f, baseSortingOrder);
        outerRing.loop = true;

        // 2. วงแหวนพลังงานชั้นใน (หมุนวนรอบตัว)
        innerRing = CreateLine("Charge_InnerRing", additiveMat, RING_SEGMENTS + 1, 0.06f, 0.06f, baseSortingOrder + 1);
        innerRing.loop = true;

        // 3. เปลวออร่า Ki พุ่งขึ้นรอบตัว
        for (int i = 0; i < FLAME_COUNT; i++)
        {
            LineRenderer flame = CreateLine($"Aura_Flame_{i}", additiveMat, 3, 0.14f, 0.01f, baseSortingOrder - 1);
            auraFlames.Add(flame);
        }

        // 4. สายฟ้าแตกเปรี๊ยะตอนพลังใกล้เต็ม
        lightningLine = CreateLine("Aura_Lightning", additiveMat, 6, 0.06f, 0.02f, baseSortingOrder + 2);
    }

    LineRenderer CreateLine(string objName, Material mat, int posCount, float startW, float endW, int order)
    {
        GameObject obj = new GameObject(objName);
        obj.transform.SetParent(transform);
        obj.transform.localPosition = Vector3.zero;

        LineRenderer lr = obj.AddComponent<LineRenderer>();
        lr.material = mat;
        lr.positionCount = posCount;
        lr.startWidth = startW;
        lr.endWidth = endW;
        lr.useWorldSpace = true;
        lr.sortingLayerName = sortingLayer;
        lr.sortingOrder = order;
        return lr;
    }

    void Update()
    {
        // หยุดทำงานหากกำลังติด QTE Finisher
        if (FinisherManager.isQTEActive)
        {
            if (isCharging) ResetCharge();
            return;
        }

        if (Input.GetMouseButton(0))
        {
            holdTimer += Time.deltaTime;

            if (holdTimer >= holdThreshold)
            {
                if (!isCharging)
                {
                    isCharging = true;
                    SetAuraActive(true);
                }

                float chargeProgress = Mathf.Clamp01((holdTimer - holdThreshold) / (maxChargeTime - holdThreshold));
                UpdateAuraVisuals(chargeProgress);
            }
        }
        else if (Input.GetMouseButtonUp(0))
        {
            if (isCharging)
            {
                // ยิงคลื่นวงแหวนระเบิดออกจังหวะปล่อยคลิก
                float finalCharge = Mathf.Clamp01((holdTimer - holdThreshold) / (maxChargeTime - holdThreshold));
                SpawnReleaseShockwave(finalCharge >= 0.9f);
            }
            ResetCharge();
        }
    }

    void UpdateAuraVisuals(float progress)
    {
        Vector3 center = transform.position + new Vector3(0f, 0.8f, 0f);
        Color currentColor = Color.Lerp(startChargeColor, fullChargeColor, progress);

        // 1. วงนอกบีบอัดพลังงานเข้าหาตัวละคร + สั่นสะเทือนเมื่อเต็มหลอด
        float pulse = progress >= 0.98f ? Mathf.Sin(Time.time * 45f) * 0.12f : 0f;
        float outerRadius = Mathf.Lerp(2.2f, 0.95f, progress) + pulse;
        DrawCircle(outerRing, center, outerRadius, Time.time * 4f, currentColor);
        outerRing.startWidth = Mathf.Lerp(0.06f, 0.18f, progress);
        outerRing.endWidth = outerRing.startWidth;

        // 2. วงในหมุนสวนทาง
        float innerRadius = Mathf.Lerp(0.5f, 1.35f, progress);
        DrawCircle(innerRing, center, innerRadius, -Time.time * 7f, Color.white);

        // 3. เปลวไฟ Ki พุ่งขึ้นจากพื้นรอบตัว
        for (int i = 0; i < auraFlames.Count; i++)
        {
            LineRenderer flame = auraFlames[i];
            float angle = ((float)i / auraFlames.Count) * Mathf.PI * 2f + (Time.time * 3f);
            float spread = Mathf.Lerp(0.4f, 0.9f, progress);
            float xOffset = Mathf.Cos(angle) * spread;

            float wave = Mathf.Repeat(Time.time * (3.5f + progress * 3f) + (i * 0.37f), 1f);
            float height = Mathf.Lerp(0.8f, 2.4f, progress) * (0.6f + 0.4f * Mathf.Sin(Time.time * 15f + i));

            Vector3 basePos = transform.position + new Vector3(xOffset, wave * 0.4f, 0f);
            Vector3 midPos = basePos + new Vector3(Mathf.Sin(Time.time * 20f + i) * 0.15f, height * 0.5f, 0f);
            Vector3 topPos = basePos + new Vector3(0f, height, 0f);

            flame.SetPosition(0, basePos);
            flame.SetPosition(1, midPos);
            flame.SetPosition(2, topPos);

            Color flameStart = currentColor;
            flameStart.a = 0.85f;
            Color flameEnd = Color.white;
            flameEnd.a = 0f;
            flame.startColor = flameStart;
            flame.endColor = flameEnd;
        }

        // 4. สายฟ้าแตกเปรี๊ยะเมื่อชาร์จเกิน 65%
        if (progress >= 0.65f)
        {
            lightningLine.enabled = true;
            lightningTimer -= Time.deltaTime;
            if (lightningTimer <= 0f)
            {
                lightningTimer = 0.05f;
                DrawJaggedLightning(center, outerRadius * 1.25f);
            }
        }
        else
        {
            lightningLine.enabled = false;
        }
    }

    void DrawCircle(LineRenderer lr, Vector3 center, float radius, float angleOffset, Color col)
    {
        lr.startColor = col;
        lr.endColor = col;

        for (int i = 0; i <= RING_SEGMENTS; i++)
        {
            float rad = ((float)i / RING_SEGMENTS) * Mathf.PI * 2f + angleOffset;
            Vector3 pos = center + new Vector3(Mathf.Cos(rad) * radius, Mathf.Sin(rad) * radius, 0f);
            lr.SetPosition(i, pos);
        }
    }

    void DrawJaggedLightning(Vector3 center, float radius)
    {
        lightningLine.startColor = lightningColor;
        lightningLine.endColor = fullChargeColor;

        float startAngle = Random.Range(0f, Mathf.PI * 2f);
        float arcSpan = Random.Range(0.8f, 1.8f);

        for (int i = 0; i < lightningLine.positionCount; i++)
        {
            float t = (float)i / (lightningLine.positionCount - 1);
            float ang = startAngle + (arcSpan * t);
            float jitter = Random.Range(-0.25f, 0.25f);
            Vector3 pos = center + new Vector3(Mathf.Cos(ang) * (radius + jitter), Mathf.Sin(ang) * (radius + jitter), 0f);
            lightningLine.SetPosition(i, pos);
        }
    }

    void SpawnReleaseShockwave(bool isMaxCharged)
    {
        GameObject waveObj = new GameObject("ChargeReleaseShockwave");
        waveObj.transform.position = transform.position + new Vector3(0f, 0.8f, 0f);

        LineRenderer lr = waveObj.AddComponent<LineRenderer>();
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.positionCount = RING_SEGMENTS + 1;
        lr.loop = true;
        lr.startWidth = isMaxCharged ? 0.25f : 0.12f;
        lr.endWidth = lr.startWidth;
        lr.startColor = isMaxCharged ? fullChargeColor : startChargeColor;
        lr.endColor = lr.startColor;
        lr.sortingLayerName = sortingLayer;
        lr.sortingOrder = baseSortingOrder + 5;

        ShockwaveExpand expand = waveObj.AddComponent<ShockwaveExpand>();
        expand.maxRadius = isMaxCharged ? 4.2f : 2.2f;
        expand.duration = 0.22f;
    }

    void SetAuraActive(bool active)
    {
        if (outerRing != null) outerRing.enabled = active;
        if (innerRing != null) innerRing.enabled = active;
        if (lightningLine != null) lightningLine.enabled = false;
        foreach (var flame in auraFlames)
        {
            if (flame != null) flame.enabled = active;
        }
    }

    void ResetCharge()
    {
        holdTimer = 0f;
        isCharging = false;
        SetAuraActive(false);
    }
}

// คลาสเสริมสำหรับขยายวงคลื่นกระแทกตอนปล่อยปุ่มชาร์จ
public class ShockwaveExpand : MonoBehaviour
{
    public float maxRadius = 4f;
    public float duration = 0.22f;
    private float timer = 0f;
    private LineRenderer lr;

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
    }

    void Update()
    {
        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / duration);
        float currentRadius = Mathf.Lerp(0.5f, maxRadius, t);

        Color c = lr.startColor;
        c.a = 1f - t;
        lr.startColor = c;
        lr.endColor = c;

        int segs = lr.positionCount - 1;
        for (int i = 0; i <= segs; i++)
        {
            float rad = ((float)i / segs) * Mathf.PI * 2f;
            lr.SetPosition(i, transform.position + new Vector3(Mathf.Cos(rad) * currentRadius, Mathf.Sin(rad) * currentRadius, 0f));
        }

        if (t >= 1f) Destroy(gameObject);
    }
}