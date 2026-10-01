using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public static CameraFollow Instance;

    [Header("Target & Following")]
    public Transform target;
    public float smoothSpeed = 14f;
    public float horizontalFollowSpeed = 10f;

    [Header("Subway Surfers Camera Offsets")]
    // Landscape offsets (PC / 16:9)
    public Vector3 landscapeOffset = new Vector3(0f, 3.5f, -5.4f);
    public float landscapePitch = 14.5f;
    public float landscapeFov = 60f;

    // Portrait offsets (Mobile / 9:16)
    public Vector3 portraitOffset = new Vector3(0f, 3.8f, -5.6f);
    public float portraitPitch = 15.5f;
    public float portraitFov = 68f;

    // Compatibility property for scene builders/inspectors
    public Vector3 offset
    {
        get { return landscapeOffset; }
        set { landscapeOffset = value; }
    }

    [Header("Screen Shake")]
    private float shakeTimer = 0f;
    private float shakeMagnitude = 0.35f;

    private Camera cam;
    private float currentX = 0f;
    private float currentBaseY = 0f;

    void Awake()
    {
        Instance = this;
        cam = GetComponent<Camera>();
    }

    void Start()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                PlayerController pc = Object.FindFirstObjectByType<PlayerController>();
                if (pc != null) player = pc.gameObject;
            }
            if (player != null) target = player.transform;
        }

        if (target != null)
        {
            currentX = target.position.x * 0.70f;
            currentBaseY = target.position.y > 2.0f ? target.position.y : 0f;

            bool isPortrait = cam != null ? cam.aspect < 1.0f : Screen.width < Screen.height;
            Vector3 initOffset = isPortrait ? portraitOffset : landscapeOffset;
            float initPitch = isPortrait ? portraitPitch : landscapePitch;

            transform.position = new Vector3(currentX + initOffset.x, currentBaseY + initOffset.y, target.position.z + initOffset.z);
            transform.rotation = Quaternion.Euler(initPitch, 0f, 0f);
        }
    }

    public void Shake(float duration = 0.22f, float magnitude = 0.35f)
    {
        shakeTimer = duration;
        shakeMagnitude = magnitude;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // 1. Detect aspect ratio (Portrait vs Landscape)
        bool isPortrait = cam != null ? cam.aspect < 1.0f : Screen.width < Screen.height;
        Vector3 activeOffset = isPortrait ? portraitOffset : landscapeOffset;
        float activePitch = isPortrait ? portraitPitch : landscapePitch;
        float activeFov = isPortrait ? portraitFov : landscapeFov;

        // 2. Adjust FOV smoothly
        if (cam != null)
        {
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, activeFov, 6f * Time.deltaTime);
        }

        // 3. Smooth horizontal lane follow (elastic response, character leads the lane change)
        float targetX = target.position.x * 0.70f;
        currentX = Mathf.Lerp(currentX, targetX, horizontalFollowSpeed * Time.deltaTime);

        // 4. Cushioned vertical elevation:
        // When running on the ground, camera stays rock-steady at ground level so jump arcs
        // look exciting and acrobatic. If the player climbs onto a train roof (y > 2.0m),
        // the camera smoothly elevates to match the train roof height.
        float targetBaseY = target.position.y > 2.0f ? target.position.y : 0f;
        currentBaseY = Mathf.Lerp(currentBaseY, targetBaseY, 6f * Time.deltaTime);

        // 5. Compute target position
        Vector3 desiredPosition = new Vector3(
            currentX + activeOffset.x,
            currentBaseY + activeOffset.y,
            target.position.z + activeOffset.z
        );

        // 6. Apply camera shake if active
        if (shakeTimer > 0f)
        {
            shakeTimer -= Time.deltaTime;
            Vector3 shakeOffset = Random.insideUnitSphere * shakeMagnitude;
            shakeOffset.z = 0f;
            desiredPosition += shakeOffset;
        }

        // 7. Apply position and fixed forward Subway Surfers rotation
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        // Fixed forward horizon: Camera NEVER pivots or twists diagonally during lane changes!
        transform.rotation = Quaternion.Euler(activePitch, 0f, 0f);
    }
}
