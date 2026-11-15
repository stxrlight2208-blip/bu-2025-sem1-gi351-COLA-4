using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FinisherManager : MonoBehaviour
{
    public static FinisherManager instance;
    public static bool isQTEActive = false;

    [Header("Sequence QTE Settings")]
    public int sequenceLength = 4;
    public float qteTimeLimit = 4.5f;
    public float wrongKeyPenalty = 0.4f;

    private readonly KeyCode[] possibleKeys = new KeyCode[]
    {
        KeyCode.Q, KeyCode.E, KeyCode.F, KeyCode.G, KeyCode.Z, KeyCode.X
    };

    private GameObject qtePanel;
    private Image keyBoxBg;
    private Text keyPromptText;
    private Text stepCounterText;
    private Image timeBarFill;

    void Awake()
    {
        instance = this;
        isQTEActive = false;
        BuildSequenceQTEUI();
    }

    void BuildSequenceQTEUI()
    {
        GameObject canvasObj = new GameObject("FinisherQTE_Canvas");
        canvasObj.transform.SetParent(transform);
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        qtePanel = new GameObject("QTE_SequencePanel");
        qtePanel.transform.SetParent(canvasObj.transform, false);
        RectTransform panelRect = qtePanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.72f);
        panelRect.anchorMax = new Vector2(0.5f, 0.72f);
        panelRect.sizeDelta = new Vector2(460f, 220f);

        Image panelBg = qtePanel.AddComponent<Image>();
        panelBg.color = new Color(0.05f, 0.05f, 0.08f, 0.85f);

        GameObject titleObj = new GameObject("TitleText");
        titleObj.transform.SetParent(qtePanel.transform, false);
        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchoredPosition = new Vector2(0f, 80f);
        titleRect.sizeDelta = new Vector2(440f, 40f);

        Text titleText = titleObj.AddComponent<Text>();
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 26;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = new Color(1f, 0.3f, 0.3f, 1f);
        titleText.text = "FINISH HIM! กดปุ่มที่ขึ้นบนจอ!";

        GameObject keyBoxObj = new GameObject("KeyBox");
        keyBoxObj.transform.SetParent(qtePanel.transform, false);
        RectTransform keyBoxRect = keyBoxObj.AddComponent<RectTransform>();
        keyBoxRect.anchoredPosition = new Vector2(0f, 10f);
        keyBoxRect.sizeDelta = new Vector2(105f, 105f);

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
        keyPromptText.fontSize = 62;
        keyPromptText.fontStyle = FontStyle.Bold;
        keyPromptText.alignment = TextAnchor.MiddleCenter;
        keyPromptText.color = new Color(0.1f, 0.1f, 0.1f, 1f);
        keyPromptText.text = "E";

        GameObject stepObj = new GameObject("StepText");
        stepObj.transform.SetParent(qtePanel.transform, false);
        RectTransform stepRect = stepObj.AddComponent<RectTransform>();
        stepRect.anchoredPosition = new Vector2(0f, -60f);
        stepRect.sizeDelta = new Vector2(400f, 30f);

        stepCounterText = stepObj.AddComponent<Text>();
        stepCounterText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        stepCounterText.fontSize = 22;
        stepCounterText.fontStyle = FontStyle.Bold;
        stepCounterText.alignment = TextAnchor.MiddleCenter;
        stepCounterText.color = Color.white;

        GameObject barBgObj = new GameObject("TimeBarBg");
        barBgObj.transform.SetParent(qtePanel.transform, false);
        RectTransform barBgRect = barBgObj.AddComponent<RectTransform>();
        barBgRect.anchoredPosition = new Vector2(0f, -90f);
        barBgRect.sizeDelta = new Vector2(400f, 16f);
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

    public void StartFinisherQTE(GameObject playerObj, GameObject targetEnemy, System.Action onSuccess, System.Action onFail)
    {
        if (isQTEActive) return;
        StartCoroutine(RunSequenceQTE(playerObj, targetEnemy, onSuccess, onFail));
    }

    private IEnumerator RunSequenceQTE(GameObject playerObj, GameObject targetEnemy, System.Action onSuccess, System.Action onFail)
    {
        isQTEActive = true;
        qtePanel.SetActive(true);

        PlayerHealth playerHealth = playerObj.GetComponent<PlayerHealth>();
        if (playerHealth != null) playerHealth.isInvincible = true;

        Rigidbody2D playerRb = playerObj.GetComponent<Rigidbody2D>();
        if (playerRb != null) playerRb.linearVelocity = Vector2.zero;

        Animator playerAnim = playerObj.GetComponent<Animator>();

        float side = playerObj.transform.position.x < targetEnemy.transform.position.x ? -1f : 1f;
        float facingDir = side < 0 ? 1f : -1f;
        playerObj.transform.position = new Vector3(targetEnemy.transform.position.x + (side * 1.1f), playerObj.transform.position.y, playerObj.transform.position.z);
        playerObj.transform.localScale = new Vector3(facingDir, 1f, 1f);

        if (CameraFollow.instance != null)
        {
            CameraFollow.instance.SetZoomOffset(-2.0f);
            CameraFollow.instance.Shake(0.15f, 0.15f);
        }

        KeyCode[] sequence = new KeyCode[sequenceLength];
        KeyCode lastPicked = KeyCode.None;
        for (int i = 0; i < sequenceLength; i++)
        {
            KeyCode picked;
            do
            {
                picked = possibleKeys[Random.Range(0, possibleKeys.Length)];
            } while (picked == lastPicked);
            sequence[i] = picked;
            lastPicked = picked;
        }

        yield return null;

        int currentIndex = 0;
        float remainingTime = qteTimeLimit;
        bool success = false;

        UpdateKeyDisplay(sequence[currentIndex], currentIndex + 1, sequenceLength);

        while (remainingTime > 0f && currentIndex < sequenceLength)
        {
            remainingTime -= Time.unscaledDeltaTime;
            timeBarFill.fillAmount = Mathf.Clamp01(remainingTime / qteTimeLimit);
            timeBarFill.color = remainingTime <= 1.2f ? Color.red : new Color(0.2f, 0.95f, 0.4f, 1f);

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

                SpawnCrossSlashLine(targetEnemy.transform.position);

                if (AudioManager.instance != null)
                    AudioManager.instance.PlaySFX(AudioManager.instance.hitImpactClip);

                if (CameraFollow.instance != null)
                    CameraFollow.instance.Shake(0.1f, 0.16f);

                DamagePopup.Create(targetEnemy.transform.position, Random.Range(3, 6), false);

                if (currentIndex >= sequenceLength)
                {
                    success = true;
                    break;
                }
                else
                {
                    UpdateKeyDisplay(sequence[currentIndex], currentIndex + 1, sequenceLength);
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
                            CameraFollow.instance.Shake(0.08f, 0.1f);
                        break;
                    }
                }
            }

            keyBoxBg.transform.localScale = Vector3.Lerp(keyBoxBg.transform.localScale, Vector3.one, 15f * Time.unscaledDeltaTime);
            keyBoxBg.color = Color.Lerp(keyBoxBg.color, new Color(1f, 0.8f, 0.1f, 1f), 10f * Time.unscaledDeltaTime);

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

            SpawnFinalPierceLine(targetEnemy.transform.position, facingDir);

            Vector3 startPos = playerObj.transform.position;
            Vector3 behindEnemyPos = new Vector3(targetEnemy.transform.position.x + (facingDir * 2.2f), startPos.y, startPos.z);

            float dashAnimTime = 0.12f;
            float t = 0f;
            while (t < dashAnimTime)
            {
                playerObj.transform.position = Vector3.Lerp(startPos, behindEnemyPos, t / dashAnimTime);
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            playerObj.transform.position = behindEnemyPos;
            if (playerAnim != null) playerAnim.SetBool("IsDashing", false);

            yield return new WaitForSecondsRealtime(0.25f);

            if (CameraFollow.instance != null)
            {
                CameraFollow.instance.ResetZoom();
                CameraFollow.instance.Shake(0.35f, 0.45f);
            }

            DamagePopup.Create(targetEnemy.transform.position + Vector3.up * 0.5f, 99, true);
            onSuccess?.Invoke();
        }
        else
        {
            if (CameraFollow.instance != null) CameraFollow.instance.ResetZoom();
            onFail?.Invoke();
        }

        yield return new WaitForSeconds(0.2f);
        if (playerHealth != null) playerHealth.isInvincible = false;
        isQTEActive = false;
    }

    void UpdateKeyDisplay(KeyCode key, int currentStep, int totalSteps)
    {
        keyPromptText.text = key.ToString();
        stepCounterText.text = $"ปุ่มที่ {currentStep} / {totalSteps}";
        keyBoxBg.color = Color.white;
        keyBoxBg.transform.localScale = Vector3.one * 1.25f;
    }

    void SpawnCrossSlashLine(Vector3 centerPos)
    {
        GameObject lineObj = new GameObject("CrossSlashVFX");
        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startWidth = 0.12f;
        lr.endWidth = 0.02f;
        lr.startColor = Color.cyan;
        lr.endColor = Color.white;
        lr.sortingOrder = 95;
        lr.positionCount = 2;

        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        Vector3 dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * 1.3f;
        lr.SetPosition(0, centerPos - dir);
        lr.SetPosition(1, centerPos + dir);

        Destroy(lineObj, 0.15f);
    }

    void SpawnFinalPierceLine(Vector3 centerPos, float dir)
    {
        GameObject lineObj = new GameObject("FinalPierceVFX");
        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startWidth = 0.35f;
        lr.endWidth = 0.05f;
        lr.startColor = new Color(1f, 0.85f, 0.1f, 1f);
        lr.endColor = Color.white;
        lr.sortingOrder = 100;
        lr.positionCount = 2;

        lr.SetPosition(0, centerPos - new Vector3(dir * 3.5f, 0f, 0f));
        lr.SetPosition(1, centerPos + new Vector3(dir * 3.5f, 0f, 0f));

        Destroy(lineObj, 0.35f);
    }
}