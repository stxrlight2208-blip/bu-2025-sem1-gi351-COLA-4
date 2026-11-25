
using System.Collections;
using UnityEngine;
using TMPro;

public class BossDialogue : MonoBehaviour
{
    [Header("Dialogue UI")]
    public GameObject dialoguePanel;
    public TMP_Text speakerName;
    public TMP_Text dialogueText;
    public TMP_Text continueText;

    [Header("Dialogue")]
    public string[] dialogueLines =
    {
        "Hah... So, you've defeated me.",
        "But you have no idea what awaits you...",
        "This is only the beginning."
    };

    private int currentLine;
    private bool isPlaying = false;

    public bool dialogueFinished = false;

    void Awake()
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
    }

    public IEnumerator PlayDialogue()
    {
        if (isPlaying) yield break;

        if (dialoguePanel == null ||
            speakerName == null ||
            dialogueText == null ||
            continueText == null)
        {
            Debug.LogError("BossDialogue UI references are missing!");
            yield break;
        }

        isPlaying = true;
        dialogueFinished = false;
        currentLine = 0;

        dialoguePanel.SetActive(true);
        speakerName.text = "BOSS";
        continueText.text = "PRESS SPACE TO CONTINUE";

        while (currentLine < dialogueLines.Length)
        {
            dialogueText.text = dialogueLines[currentLine];

            // รอให้ผู้เล่นกด Space
            while (!Input.GetKeyDown(KeyCode.Space))
            {
                yield return null;
            }

            // ป้องกันการอ่าน Space ซ้ำในเฟรมเดียวกัน
            yield return null;

            currentLine++;
        }

        dialoguePanel.SetActive(false);

        dialogueFinished = true;
        isPlaying = false;
    }
}
