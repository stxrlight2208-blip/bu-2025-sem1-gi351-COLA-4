using System.Collections;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public static CameraFollow instance;

    [Header("Target Settings")]
    public Transform target;

    [Header("Camera Settings")]
    public float smoothSpeed = 10f;
    public Vector3 offset = new Vector3(0f, 1f, -10f);

    [Header("Zoom Settings")]
    public float zoomSpeed = 6f;
    private Camera cam;
    private float defaultZoom;
    private float targetZoom;

    private Vector3 shakeOffset = Vector3.zero;
    private Coroutine shakeCoroutine;

    void Awake()
    {
        instance = this;
        cam = GetComponent<Camera>();
        if (cam != null)
        {
            defaultZoom = cam.orthographicSize;
            targetZoom = defaultZoom;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = target.position + offset;
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        smoothedPosition += shakeOffset;
        smoothedPosition.z = offset.z;
        transform.position = smoothedPosition;

        if (cam != null && cam.orthographic)
        {
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetZoom, zoomSpeed * Time.unscaledDeltaTime);
        }
    }

    public void SetZoomOffset(float zoomOffset)
    {
        if (cam != null)
        {
            targetZoom = Mathf.Max(2f, defaultZoom + zoomOffset);
        }
    }

    public void ResetZoom()
    {
        targetZoom = defaultZoom;
    }

    public void Shake(float duration, float magnitude)
    {
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
        }
        shakeCoroutine = StartCoroutine(DoShake(duration, magnitude));
    }

    private IEnumerator DoShake(float duration, float magnitude)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            shakeOffset = new Vector3(x, y, 0f);

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        shakeOffset = Vector3.zero;
    }
}