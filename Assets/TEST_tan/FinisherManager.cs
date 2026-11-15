using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FinisherManager : MonoBehaviour
{
    public static FinisherManager instance;
    public static bool isQTEActive = false;

    [Header("Sequence QTE Settings (สุ่มอิสระ + ซ้ำปุ่มได้)")]
    public int normalEnemySteps = 2;
    public float normalEnemyTime = 1.6f;
    public int bossSteps = 5;
    public float bossTime = 5.0f;
    public float wrongKeyPenalty = 0.25f;

    private readonly KeyCode[] possibleKeys = new KeyCode[]
    {
        KeyCode.Q, KeyCode.E, KeyCode.F, KeyCode.R, KeyCode.C
    };

    private Canvas mainCanvas;
    private RectTransform qtePanelRect;
    private GameObject qtePanel;
    private Image keyBoxBg;
    private Text keyPromptText;
    private Text stepCounterText;
    private Image timeBarFill;

    void Awake()
    {
        instance = this;
        isQTEActive = false;
        BuildWorldFollowQTEUI();
    }

    void BuildWorldFollowQTEUI()
    {
        GameObject canvasObj = new GameObject("FinisherQTE_Canvas");
        canvasObj.transform.SetParent(transform);
        mainCanvas = canvasObj.AddComponent<Canvas>();
        mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        mainCanvas.sortingOrder = 200;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        qtePanel = new GameObject("QTE_SidePanel");
        qtePanel.transform.SetParent(canvasObj.transform, false);
        qtePanelRect = qtePanel.AddComponent<RectTransform>();
        qtePanelRect.sizeDelta = new Vector2(150f, 165f);

        Image panelBg = qtePanel.AddComponent<Image>();
        panelBg.color = new Color(0.05f, 0.05f, 0.08f, 0.82f);

        GameObject keyBoxObj = new GameObject("KeyBox");
        keyBoxObj.transform.SetParent(qtePanel.transform, false);
        RectTransform keyBoxRect = keyBoxObj.AddComponent<RectTransform>();
        keyBoxRect.anchoredPosition = new Vector2(0f, 22f);
        keyBoxRect.sizeDelta = new Vector2(88f, 88f);

        keyBoxBg = keyBoxObj.AddComponent<Image>();
        keyBoxBg.color = new Color(1f, 0.8f, 0.1f, 1f);

        GameObject keyTextObj = new GameObject("KeyText");
        keyTextObj.transform.SetParent(keyBoxObj.transform, false);
        RectTransform keyTextRect = keyTextObj.AddComponent<RectTransform>();
        keyTextRect.anchorMin = Vector2.zero;
        keyTextRect.anchorMax = Vector2.one;
        keyTextRect.offsetMin = Vector2.zero;
        keyTextRect.offsetMax = Vector2.zero;

        keyPromptText = keyTextObj.AddComponent<Text>();
        keyPromptText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        keyPromptText.fontSize = 54;
        keyPromptText.fontStyle = FontStyle.Bold;
        keyPromptText.alignment = TextAnchor.MiddleCenter;
        keyPromptText.color = new Color(0.1f, 0.1f, 0.1f, 1f);
        keyPromptText.text = "E";

        GameObject stepObj = new GameObject("StepText");
        stepObj.transform.SetParent(qtePanel.transform, false);
        RectTransform stepRect = stepObj.AddComponent<RectTransform>();
        stepRect.anchoredPosition = new Vector2(0f, -38f);
        stepRect.sizeDelta = new Vector2(140f, 26f);

        stepCounterText = stepObj.AddComponent<Text>();
        stepCounterText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        stepCounterText.fontSize = 20;
        stepCounterText.fontStyle = FontStyle.Bold;
        stepCounterText.alignment = TextAnchor.MiddleCenter;
        stepCounterText.color = Color.white;

        GameObject barBgObj = new GameObject("TimeBarBg");
        barBgObj.transform.SetParent(qtePanel.transform, false);
        RectTransform barBgRect = barBgObj.AddComponent<RectTransform>();
        barBgRect.anchoredPosition = new Vector2(0f, -62f);
        barBgRect.sizeDelta = new Vector2(120f, 12f);
        Image barBgImg = barBgObj.AddComponent<Image>();
        barBgImg.color = new Color(0.25f, 0.25f, 0.25f, 1f);

        GameObject barFillObj = new GameObject("TimeBarFill");
        barFillObj.transform.SetParent(barBgObj.transform, false);
        RectTransform fillRect = barFillObj.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        timeBarFill = barFillObj.AddComponent<Image>();
        timeBarFill.color = new Color(0.2f, 0.95f, 0.4f, 1f);
        timeBarFill.type = Image.Type.Filled;
        timeBarFill.fillMethod = Image.FillMethod.Horizontal;
        timeBarFill.fillAmount = 1f;

        qtePanel.SetActive(false);
    }

    public void StartFinisherQTE(GameObject playerObj, GameObject targetEnemy, bool isBoss, System.Action onSuccess, System.Action onFail)
    {
        if (isQTEActive) return;
        StartCoroutine(RunSequenceQTE(playerObj, targetEnemy, isBoss, onSuccess, onFail));
    }

    private IEnumerator RunSequenceQTE(GameObject playerObj, GameObject targetEnemy, bool isBoss, System.Action onSuccess, System.Action onFail)
    {
        isQTEActive = true;
        qtePanel.SetActive(true);

        int steps = isBoss ? bossSteps : normalEnemySteps;
        float timeLimit = isBoss ? bossTime : normalEnemyTime;

        PlayerHealth playerHealth = playerObj.GetComponent<PlayerHealth>();
        if (playerHealth != null) playerHealth.isInvincible = true;

        Rigidbody2D playerRb = playerObj.GetComponent<Rigidbody2D>();
        if (playerRb != null) playerRb.linearVelocity = Vector2.zero;

        Animator playerAnim = playerObj.GetComponent<Animator>();

        float side = playerObj.transform.position.x < targetEnemy.transform.position.x ? -1f : 1f;
        float facingDir = side < 0 ? 1f : -1f;
        float standOffset = isBoss ? 1.5f : 1.1f;
        playerObj.transform.position = new Vector3(targetEnemy.transform.position.x + (side * standOffset), playerObj.transform.position.y, playerObj.transform.position.z);
        playerObj.transform.localScale = new Vector3(facingDir, 1f, 1f);

        if (CameraFollow.instance != null)
        {
            CameraFollow.instance.SetZoomOffset(isBoss ? -2.2f : -1.4f);
            CameraFollow.instance.Shake(0.12f, isBoss ? 0.18f : 0.1f);
        }

        KeyCode[] sequence = new KeyCode[steps];
        for (int i = 0; i < steps; i++)
        {
            sequence[i] = possibleKeys[Random.Range(0, possibleKeys.Length)];
        }

        yield return null;

        int currentIndex = 0;
        float remainingTime = timeLimit;
        bool success = false;

        UpdateKeyDisplay(sequence[currentIndex], currentIndex + 1, steps, isBoss, false);

        while (remainingTime > 0f && currentIndex < steps)
        {
            UpdateUIPositionBesidePlayer(playerObj.transform.position, side);

            remainingTime -= Time.unscaledDeltaTime;
            timeBarFill.fillAmount = Mathf.Clamp01(remainingTime / timeLimit);
            timeBarFill.color = (remainingTime / timeLimit) <= 0.35f ? Color.red : new Color(0.2f, 0.95f, 0.4f, 1f);

            KeyCode targetKey = sequence[currentIndex];

            if (Input.GetKeyDown(targetKey))
            {
                currentIndex++;

                if (playerAnim != null)
                {
                    int step = currentIndex % 3;
                    if (step == 1) playerAnim.SetTrigger("Slash");
                    else if (step == 2) playerAnim.SetTrigger("Upper");
                    else playerAnim.SetTrigger("Heavy");
                }

                SpawnCrossSlashLine(targetEnemy.transform.position, isBoss);

                if (AudioManager.instance != null)
                    AudioManager.instance.PlaySFX(AudioManager.instance.hitImpactClip);

                if (CameraFollow.instance != null)
                    CameraFollow.instance.Shake(0.08f, isBoss ? 0.18f : 0.12f);

                int qteStepDmg = Random.Range(3, 6);
                DamagePopup.Create(targetEnemy.transform.position, qteStepDmg, false);
                ComboCounterUI.RegisterHit(qteStepDmg);

                if (currentIndex >= steps)
                {
                    success = true;
                    break;
                }
                else
                {
                    bool isRepeatedKey = (sequence[currentIndex] == targetKey);
                    UpdateKeyDisplay(sequence[currentIndex], currentIndex + 1, steps, isBoss, isRepeatedKey);
                }
            }
            else
            {
                foreach (KeyCode k in possibleKeys)
                {
                    if (k != targetKey && Input.GetKeyDown(k))
                    {
                        remainingTime -= wrongKeyPenalty;
                        keyBoxBg.color = Color.red;
                        if (CameraFollow.instance != null)
                            CameraFollow.instance.Shake(0.06f, 0.08f);
                        break;
                    }
                }
            }

            Color targetColor = isBoss ? new Color(1f, 0.35f, 0.25f, 1f) : new Color(1f, 0.8f, 0.1f, 1f);
            keyBoxBg.transform.localScale = Vector3.Lerp(keyBoxBg.transform.localScale, Vector3.one, 18f * Time.unscaledDeltaTime);
            keyBoxBg.color = Color.Lerp(keyBoxBg.color, targetColor, 12f * Time.unscaledDeltaTime);

            yield return null;
        }

        qtePanel.SetActive(false);

        if (success)
        {
            if (playerAnim != null)
            {
                playerAnim.SetBool("IsDashing", true);
                playerAnim.SetTrigger("Combo");
            }

            if (AudioManager.instance != null)
                AudioManager.instance.PlaySFX(AudioManager.instance.dashClip);

            SpawnFinalPierceLine(targetEnemy.transform.position, facingDir, isBoss);

            Vector3 startPos = playerObj.transform.position;
            float passDistance = isBoss ? 3.2f : 2.2f;
            Vector3 behindEnemyPos = new Vector3(targetEnemy.transform.position.x + (facingDir * passDistance), startPos.y, startPos.z);

            float dashAnimTime = 0.1f;
            float t = 0f;
            while (t < dashAnimTime)
            {
                playerObj.transform.position = Vector3.Lerp(startPos, behindEnemyPos, t / dashAnimTime);
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            playerObj.transform.position = behindEnemyPos;
            if (playerAnim != null) playerAnim.SetBool("IsDashing", false);

            yield return new WaitForSecondsRealtime(isBoss ? 0.35f : 0.18f);

            if (CameraFollow.instance != null)
            {
                CameraFollow.instance.ResetZoom();
                CameraFollow.instance.Shake(isBoss ? 0.45f : 0.25f, isBoss ? 0.5f : 0.3f);
            }

            int finalDmg = isBoss ? 999 : 99;
            DamagePopup.Create(targetEnemy.transform.position + Vector3.up * 0.6f, finalDmg, true);
            ComboCounterUI.RegisterHit(finalDmg);
            onSuccess?.Invoke();
        }
        else
        {
            if (CameraFollow.instance != null) CameraFollow.instance.ResetZoom();
            onFail?.Invoke();
        }

        yield return new WaitForSeconds(0.15f);
        if (playerHealth != null) playerHealth.isInvincible = false;
        isQTEActive = false;
    }

    void UpdateUIPositionBesidePlayer(Vector3 playerWorldPos, float side)
    {
        if (Camera.main == null || qtePanelRect == null) return;

        Vector3 worldAnchor = playerWorldPos + new Vector3(side * 1.3f, 1.1f, 0f);
        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldAnchor);

        screenPos.x = Mathf.Clamp(screenPos.x, 100f, Screen.width - 100f);
        screenPos.y = Mathf.Clamp(screenPos.y, 100f, Screen.height - 100f);

        qtePanelRect.position = screenPos;
    }

    void UpdateKeyDisplay(KeyCode key, int currentStep, int totalSteps, bool isBoss, bool isRepeated)
    {
        keyPromptText.text = key.ToString();
        stepCounterText.text = isBoss ? $"BOSS {currentStep}/{totalSteps}" : $"{currentStep} / {totalSteps}";
        keyBoxBg.color = isRepeated ? Color.cyan : Color.white;
        keyBoxBg.transform.localScale = Vector3.one * (isRepeated ? 1.42f : 1.28f);
    }

    void SpawnCrossSlashLine(Vector3 centerPos, bool isBoss)
    {
        GameObject lineObj = new GameObject("CrossSlashVFX");
        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startWidth = isBoss ? 0.18f : 0.12f;
        lr.endWidth = 0.02f;
        lr.startColor = isBoss ? new Color(1f, 0.3f, 0.2f, 1f) : Color.cyan;
        lr.endColor = Color.white;
        lr.sortingOrder = 95;
        lr.positionCount = 2;

        float radius = isBoss ? 2.0f : 1.3f;
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        Vector3 dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;
        lr.SetPosition(0, centerPos - dir);
        lr.SetPosition(1, centerPos + dir);

        Destroy(lineObj, 0.15f);
    }

    void SpawnFinalPierceLine(Vector3 centerPos, float dir, bool isBoss)
    {
        GameObject lineObj = new GameObject("FinalPierceVFX");
        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startWidth = isBoss ? 0.5f : 0.35f;
        lr.endWidth = 0.05f;
        lr.startColor = new Color(1f, 0.85f, 0.1f, 1f);
        lr.endColor = Color.white;
        lr.sortingOrder = 100;
        lr.positionCount = 2;

        float length = isBoss ? 5.0f : 3.5f;
        lr.SetPosition(0, centerPos - new Vector3(dir * length, 0f, 0f));
        lr.SetPosition(1, centerPos + new Vector3(dir * length, 0f, 0f));

        Destroy(lineObj, 0.35f);
    }
}