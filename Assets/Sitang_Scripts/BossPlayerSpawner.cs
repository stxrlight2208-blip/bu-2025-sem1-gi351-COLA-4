
using UnityEngine;

public class BossPlayerSpawner : MonoBehaviour
{
    public Transform spawnPoint;

    void Start()
    {
        if (CharacterManager.Instance == null)
        {
            Debug.LogError("CharacterManager not found!");
            return;
        }

        GameObject selectedPrefab =
            CharacterManager.Instance.GetSelectedCharacter();

        if (selectedPrefab == null)
        {
            Debug.LogError("No character selected!");
            return;
        }

        Instantiate(
            selectedPrefab,
            spawnPoint.position,
            spawnPoint.rotation
        );
    }
}
