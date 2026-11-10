
using UnityEngine;

public class ParallaxBackground : MonoBehaviour
{
    public Transform player;
    public float parallaxEffect = 3.0f;

    private Vector3 startPosition;
    private float playerStartX;

    void Start()
    {
        startPosition = transform.position;
        playerStartX = player.position.x;
    }

    void LateUpdate()
    {
        if (player == null) return;

        float distance = player.position.x - playerStartX;

        transform.position = new Vector3(
            startPosition.x + distance * parallaxEffect,
            startPosition.y,
            startPosition.z
        );
    }
}
