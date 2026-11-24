using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class CharacterManager : MonoBehaviour
{
    public static CharacterManager Instance;

    [Header("Character Prefabs")]
    public GameObject[] characterPrefabs;
    public int selectedCharacterIndex = -1;

    [Header("Character Animators")]
    public Animator blueAnimator;
    public Animator redAnimator;

    [Header("UI Warning")]
    public TMP_Text warningText;
    public float warningDuration = 2.0f; // ⭐ ระยะเวลาที่ข้อความจะโชว์ (วินาที)
    private Coroutine warningCoroutine;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // หยุด Animation ตอนเข้าหน้าเลือกตัวละคร
        if (blueAnimator != null)
            blueAnimator.speed = 0;

        if (redAnimator != null)
            redAnimator.speed = 0;

        if (warningText != null)
            warningText.gameObject.SetActive(false);
    }


    public void SelectCharacter(int index)
    {
        selectedCharacterIndex = index;

        // หยุด Animation ทั้งสองตัวก่อน
        if (blueAnimator != null)
            blueAnimator.speed = 0;

        if (redAnimator != null)
            redAnimator.speed = 0;

        // เลือกตัวละครสีน้ำเงิน
        if (index == 0)
        {
            blueAnimator.Play("Blue", 0, 0f);
            blueAnimator.speed = 1;
        }

        // เลือกตัวละครสีแดง
        else if (index == 1)
        {
            redAnimator.speed = 1;
        }

        if (warningCoroutine != null)
            StopCoroutine(warningCoroutine);

        if (warningText != null)
            warningText.gameObject.SetActive(false);
    }

    public GameObject GetSelectedCharacter()
    {
        if (selectedCharacterIndex >= 0 && selectedCharacterIndex < characterPrefabs.Length)
        {
            return characterPrefabs[selectedCharacterIndex];
        }
        return null;
    }

    public void StartGame(string sceneName)
    {
        if (selectedCharacterIndex == -1)
        {
            Debug.LogWarning("กรุณาเลือกตัวละครก่อนเริ่มเกม!");

            if (warningText != null)
            {
                // ⭐ รีเซ็ตตัวจับเวลาใหม่ทุกครั้งที่กดย้ำ ข้อความจะได้ไม่หายก่อนกำหนด
                if (warningCoroutine != null)
                {
                    StopCoroutine(warningCoroutine);
                }
                warningCoroutine = StartCoroutine(ShowWarningRoutine("Please select a character!"));
            }
            return;
        }

        SceneManager.LoadScene(sceneName);
    }

    // ⭐ ฟังก์ชันนับเวลาถอยหลังเพื่อซ่อนข้อความอัตโนมัติ
    IEnumerator ShowWarningRoutine(string message)
    {
        warningText.text = message;
        warningText.gameObject.SetActive(true);

        yield return new WaitForSeconds(warningDuration);

        warningText.gameObject.SetActive(false);
    }
}