using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public static CameraFollow Instance;
    public Transform target;
    
    // Standard offset property (Required by RunnerSceneBuilder and inspectors)
    public Vector3 offset = new Vector3(0f, 5.8f, -8.8f);

    // Dynamic offsets for Landscape vs Portrait
    public Vector3 portraitOffset = new Vector3(0f, 5.8f, -8.8f);
    public Vector3 landscapeOffset = new Vector3(0f, 4.5f, -6.5f);
    public float portraitFov = 72f;
    public float landscapeFov = 60f;
    
    public float smoothSpeed = 11f;

    private float shakeTimer = 0f;
    private float shakeMagnitude = 0.35f;
    private Camera cam;

    void Awake()
    {
        Instance = this;
        cam = GetComponent<Camera>();
    }

    public void Shake(float duration = 0.22f, float magnitude = 0.35f)
    {
        shakeTimer = duration;
        shakeMagnitude = magnitude;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Smoothly adapt to screen aspect ratio (Portrait vs Landscape)
        bool isPortrait = cam != null ? cam.aspect < 1.0f : Screen.width < Screen.height;
        Vector3 targetOffset = isPortrait ? portraitOffset : landscapeOffset;
        float targetFov = isPortrait ? portraitFov : landscapeFov;

        if (cam != null)
        {
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFov, 6f * Time.deltaTime);
        }

        Vector3 desiredPosition = new Vector3(target.position.x * 0.35f, target.position.y, target.position.z) + targetOffset;

        // Apply camera shake if active
        if (shakeTimer > 0f)
        {
            shakeTimer -= Time.deltaTime;
            Vector3 shakeOffset = Random.insideUnitSphere * shakeMagnitude;
            shakeOffset.z = 0f;
            desiredPosition += shakeOffset;
        }

        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.LookAt(target.position + Vector3.up * 1.3f);
    }
}
