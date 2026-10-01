using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private CharacterController controller;
    private Transform characterModel;
    private Transform hoverboard;
    private Animator animator;
    private Renderer[] characterRenderers;

    [Header("Movement Settings")]
    public float forwardSpeed = 12f;
    public float laneDistance = 2.5f;
    public float laneChangeSpeed = 13.5f; // Authentic Subway Surfers lateral speed
    public float laneChangeSmoothTime = 0.075f; // Responsive ~0.18-0.20s transition
    private int targetLane = 1; // 0: Left, 1: Middle, 2: Right
    private float lateralVelocityRef = 0f;

    [Header("Subway Surfers Speed Progression")]
    public float initialSpeed = 12f;
    public float maxSpeed = 24.5f;
    public float speedAcceleration = 0.12f; // Smooth acceleration over time
    public float currentBaseSpeed = 12f;
    public float hoverboardBonusSpeed = 4.0f; // Fast, exciting hover rush!

    [Header("Jump Settings")]
    public float jumpForce = 8.5f;
    public float gravity = 28f;
    private float verticalVelocity;
    private float lastGroundedTime = 0f;

    [Header("Slide / Roll Settings")]
    public float slideDuration = 0.58f; // Authentic Subway Surfers forward roll timing
    public bool isSliding = false;
    private float slideTimer = 0f;

    // Collider heights (bottom stays anchored to floor at -0.70m!)
    private float normalHeight = 1.4f;
    private Vector3 normalCenter = new Vector3(0f, 0f, 0f);
    private float slideHeight = 0.60f;
    private Vector3 slideCenter = new Vector3(0f, -0.40f, 0f);

    private float baseScale = 0.54f;

    [Header("Subway Surfers Hoverboard Powerup")]
    public bool isHoverboardActive = false;
    public float hoverboardTimer = 0f;
    public int hoverboardCount = 2; // Starts with 2 boards ready to deploy!
    public bool isInvulnerable = false;
    private float invulnerableTimer = 0f;

    [Header("Mobile Touch & Gesture Controls")]
    private Vector2 touchStartPos;
    private bool isTouching = false;
    private bool hasSwiped = false;
    private float minSwipeDistance = 30f;
    private float lastTapTime = -1f;
    private float doubleTapMaxDelay = 0.32f;

    // Visual notification
    private readonly Collider[] overlapHitBuffer = new Collider[16];
    private string bannerMessage = "";
    private float bannerTimer = 0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (controller == null) controller = gameObject.AddComponent<CharacterController>();
        controller.height = normalHeight;
        controller.radius = 0.35f;
        controller.center = normalCenter;

        characterModel = transform.Find("CharacterModel");
        hoverboard = transform.Find("Hoverboard");
        animator = GetComponentInChildren<Animator>();

        if (characterModel != null)
        {
            baseScale = characterModel.localScale.x;
            characterRenderers = characterModel.GetComponentsInChildren<Renderer>();
        }

        // Load persistent hoverboard inventory from ShopManager
        if (ShopManager.Instance != null)
        {
            hoverboardCount = ShopManager.Instance.GetHoverboardStock();
        }

        // By default in Subway Surfers, player runs on their feet!
        if (hoverboard != null)
        {
            hoverboard.gameObject.SetActive(false);
        }
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying)
        {
            if (animator != null)
            {
                animator.SetBool("IsHovering", true);
                animator.speed = 1f;
            }
        }
        else
        {
            if (animator != null)
            {
                animator.SetBool("IsHovering", false);
            }
        }
        currentBaseSpeed = initialSpeed;
        forwardSpeed = initialSpeed;
    }

    void Update()
    {
        // 0. Intro / Start Screen: Character stands idle waiting for user tap or key
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying && !GameManager.Instance.isGameOver)
        {
            if (animator != null)
            {
                animator.SetBool("IsHovering", true);
                animator.speed = 1f;
            }
            if (hoverboard != null)
            {
                hoverboard.gameObject.SetActive(false);
            }
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            {
                GameManager.Instance.StartGame();
            }
            return;
        }

        // Pause state
        if (GameManager.Instance != null && GameManager.Instance.isPaused)
        {
            return;
        }

        // Toggle pause shortcut
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
        {
            GameManager.Instance?.TogglePause();
            return;
        }

        if (GameManager.Instance != null && GameManager.Instance.isGameOver)
        {
            if (animator != null) animator.speed = 0f;
            return;
        }

        // 1. Off-Track & Fall Checks: End game if player leaves railway bed or falls
        if (Mathf.Abs(transform.position.x) > 3.6f || transform.position.y < -0.8f)
        {
            if (GameManager.Instance != null && !GameManager.Instance.isGameOver)
            {
                GameManager.Instance.GameOver();
                return;
            }
        }

        // 0. Smooth Speed Progression over time and distance
        if (GameManager.Instance != null && !GameManager.Instance.isGameOver)
        {
            currentBaseSpeed = Mathf.Min(maxSpeed, currentBaseSpeed + speedAcceleration * Time.deltaTime);
        }
        forwardSpeed = isHoverboardActive ? (currentBaseSpeed + hoverboardBonusSpeed) : currentBaseSpeed;

        // 2. Proactive Capsule Overlap Check against obstacles
        CheckObstacleOverlaps();

        // 3. Sync Animator state: When hoverboard is active, STOP running animation!
        if (animator != null)
        {
            animator.SetBool("IsHovering", isHoverboardActive);
        }

        // 4. Hoverboard Timer and Invulnerability Updates
        UpdateHoverboardState();

        // 5. Unified Input: Mobile Touch Gestures (Swipe / Double-Tap) + Keyboard Fallback
        ProcessMovementAndGestureInput();

        // 6. Authentic Subway Surfers Lateral Movement (~0.18-0.20s smooth transition)
        float maxLateralSpeed = Mathf.Max(laneChangeSpeed, forwardSpeed * 1.15f);
        float targetX = (targetLane - 1) * laneDistance;
        float currentX = transform.position.x;
        float newX = Mathf.SmoothDamp(currentX, targetX, ref lateralVelocityRef, laneChangeSmoothTime, maxLateralSpeed);
        if (Mathf.Abs(newX - targetX) < 0.005f)
        {
            newX = targetX;
            lateralVelocityRef = 0f;
        }
        float lateralVelocity = (newX - currentX) / Time.deltaTime;

        // Scale running animation tempo with player running velocity (crunched tuck during forward roll)
        if (animator != null && !isHoverboardActive)
        {
            animator.speed = isSliding ? 0.35f : Mathf.Clamp(forwardSpeed / initialSpeed, 1.0f, 1.75f);
        }

        // 7. Slide Timer & State Update
        if (isSliding)
        {
            slideTimer -= Time.deltaTime;
            if (slideTimer <= 0f)
            {
                StopSlide();
            }
        }

        // 8. Visual Model & Hoverboard Animation
        AnimatePlayerAndBoard();

        // 9. Vertical Jump & Gravity (Tight, snappy Subway Surfers jump arc)
        if (controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f; // Firmly glued to ground / train roof
            lastGroundedTime = Time.time;
        }
        else
        {
            // In air or jumping: Gravity immediately pulls player down!
            verticalVelocity -= gravity * Time.deltaTime;
        }

        // 10. Move Character - Player ALWAYS moves forward down the track!
        Vector3 move = new Vector3(lateralVelocity, verticalVelocity, forwardSpeed);
        controller.Move(move * Time.deltaTime);

        // 11. Dynamic Banking Tilt (Athletic lean into track turn)
        float tiltAngle = Mathf.Clamp(-lateralVelocity * 1.1f, -14f, 14f);
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0f, 0f, tiltAngle), 16f * Time.deltaTime);
    }

    private void ProcessMovementAndGestureInput()
    {
        // --- A. KEYBOARD FALLBACK (PC & Editor) ---
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            ChangeLane(-1);
        }
        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            ChangeLane(1);
        }
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.Space))
        {
            PerformJump();
        }
        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            PerformSlide();
        }
        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return))
        {
            DeployHoverboard();
        }

        // --- B. MOBILE TOUCH & MOUSE SWIPE GESTURES ---
        Vector2 currentTouchPos = Vector2.zero;
        bool touchBegan = false;
        bool touchEnded = false;
        bool touchOngoing = false;

        if (Input.touchCount > 0)
        {
            Touch t = Input.GetTouch(0);
            currentTouchPos = t.position;
            if (t.phase == TouchPhase.Began) touchBegan = true;
            else if (t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary) touchOngoing = true;
            else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) touchEnded = true;
        }
        else
        {
            // Mouse drag simulation (Allows testing mobile swipe controls in Unity Editor Game View!)
            if (Input.GetMouseButtonDown(0))
            {
                currentTouchPos = Input.mousePosition;
                touchBegan = true;
            }
            else if (Input.GetMouseButton(0))
            {
                currentTouchPos = Input.mousePosition;
                touchOngoing = true;
            }
            else if (Input.GetMouseButtonUp(0))
            {
                currentTouchPos = Input.mousePosition;
                touchEnded = true;
            }
        }

        if (touchBegan)
        {
            touchStartPos = currentTouchPos;
            isTouching = true;
            hasSwiped = false;
        }
        else if (touchOngoing && isTouching && !hasSwiped)
        {
            Vector2 delta = currentTouchPos - touchStartPos;
            float absX = Mathf.Abs(delta.x);
            float absY = Mathf.Abs(delta.y);
            float dist = delta.magnitude;

            float threshold = Mathf.Max(minSwipeDistance, Screen.height * 0.035f);

            if (dist > threshold)
            {
                hasSwiped = true;
                if (absX > absY)
                {
                    // Horizontal swipe: Switch lanes
                    if (delta.x > 0) ChangeLane(1);
                    else ChangeLane(-1);
                }
                else
                {
                    // Vertical swipe: Jump or Slide
                    if (delta.y > 0) PerformJump();
                    else PerformSlide();
                }
            }
        }
        else if (touchEnded && isTouching)
        {
            isTouching = false;
            if (!hasSwiped)
            {
                Vector2 delta = currentTouchPos - touchStartPos;
                float absX = Mathf.Abs(delta.x);
                float absY = Mathf.Abs(delta.y);
                float threshold = Mathf.Max(25f, Screen.height * 0.025f);

                if (delta.magnitude > threshold)
                {
                    // Fast flick swipe on release
                    hasSwiped = true;
                    if (absX > absY)
                    {
                        if (delta.x > 0) ChangeLane(1);
                        else ChangeLane(-1);
                    }
                    else
                    {
                        if (delta.y > 0) PerformJump();
                        else PerformSlide();
                    }
                }
                else
                {
                    // Tap / Double-Tap detection
                    float timeSinceLastTap = Time.time - lastTapTime;
                    if (timeSinceLastTap < doubleTapMaxDelay)
                    {
                        // Double Tap detected! Subway Surfers signature hoverboard activation!
                        DeployHoverboard();
                        lastTapTime = -1f;
                    }
                    else
                    {
                        lastTapTime = Time.time;
                    }
                }
            }
        }
    }

    public void ChangeLane(int direction)
    {
        if (direction < 0 && targetLane > 0)
        {
            targetLane--;
        }
        else if (direction > 0 && targetLane < 2)
        {
            targetLane++;
        }
    }

    public bool IsGrounded()
    {
        return controller != null && (controller.isGrounded || (Time.time - lastGroundedTime < 0.12f));
    }

    public void PerformJump()
    {
        if (IsGrounded())
        {
            if (isSliding) StopSlide();
            verticalVelocity = jumpForce;
            lastGroundedTime = -10f; // Consume jump immediately
            AudioManager.Instance?.PlayJump();
        }
    }

    public void PerformSlide()
    {
        StartSlide();
        AudioManager.Instance?.PlaySlide();
        if (controller != null && !controller.isGrounded)
        {
            // Fast dive straight back to ground!
            verticalVelocity = -18f;
        }
    }

    public void DeployHoverboard()
    {
        if (!isHoverboardActive && hoverboardCount > 0)
        {
            if (ShopManager.Instance != null)
            {
                ShopManager.Instance.ConsumeHoverboard();
                hoverboardCount = ShopManager.Instance.GetHoverboardStock();
            }
            else
            {
                hoverboardCount--;
            }
            float duration = ShopManager.Instance != null ? ShopManager.Instance.GetHoverboardDuration() : 15f;
            ActivateHoverboard(duration);
            AudioManager.Instance?.PlayHoverboardDeploy();
            ShowBanner(string.Format("🛹 HOVERBOARD ACTIVATED! ({0} Remaining)", hoverboardCount));
        }
        else if (isHoverboardActive)
        {
            ShowBanner("🛹 HOVERBOARD SHIELD ALREADY ACTIVE!");
        }
        else if (hoverboardCount <= 0)
        {
            ShowBanner("NO HOVERBOARDS! Collect them on track!");
        }
    }

    // Direct solid collider collision detection (Handles trains, barriers, hurdles)
    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (GameManager.Instance != null && GameManager.Instance.isGameOver) return;
        if (isInvulnerable) return;

        // Ignore ground, tracks, rails, tiles, ballast
        string hName = hit.gameObject.name;
        if (hName.Contains("Track") || hName.Contains("Rail") || hName.Contains("Tile") || 
            hName.Contains("Road") || hName.Contains("Ballast") || hName.Contains("Ground"))
        {
            return;
        }

        bool isObstacle = hit.gameObject.CompareTag("Obstacle") ||
                          hit.gameObject.GetComponent<Obstacle>() != null ||
                          hit.gameObject.GetComponentInParent<Obstacle>() != null ||
                          hName.Contains("Train") || hName.Contains("Barrier") || hName.Contains("Hurdle");

        if (isObstacle)
        {
            // If the player landed on top of the train roof, allow running on roof
            if (hit.normal.y > 0.65f)
            {
                return;
            }

            // Crashed into the front or side of the obstacle!
            HandleObstacleHit(hit.gameObject);
        }
    }

    private void CheckObstacleOverlaps()
    {
        if (GameManager.Instance != null && GameManager.Instance.isGameOver) return;
        if (isInvulnerable) return;

        Vector3 p1 = transform.position + controller.center + Vector3.up * (-controller.height * 0.5f + controller.radius);
        Vector3 p2 = transform.position + controller.center + Vector3.up * (controller.height * 0.5f - controller.radius);
        
        // Zero-allocation NonAlloc capsule overlap check (Prevents 60 GC allocations/sec)
        int hitCount = Physics.OverlapCapsuleNonAlloc(p1, p2, controller.radius * 0.95f, overlapHitBuffer);

        for (int i = 0; i < hitCount; i++)
        {
            Collider col = overlapHitBuffer[i];
            overlapHitBuffer[i] = null; // Clear reference immediately for memory hygiene
            if (col == null || col.gameObject == gameObject || col.transform.IsChildOf(transform)) continue;

            string cName = col.gameObject.name;
            if (cName.Contains("Track") || cName.Contains("Rail") || cName.Contains("Tile") || 
                cName.Contains("Road") || cName.Contains("Ballast") || cName.Contains("Coin") || 
                cName.Contains("Pickup") || cName.Contains("Hoverboard"))
            {
                continue;
            }

            bool isObstacle = col.CompareTag("Obstacle") ||
                              col.GetComponent<Obstacle>() != null ||
                              col.GetComponentInParent<Obstacle>() != null ||
                              cName.Contains("Train") || cName.Contains("Barrier") || cName.Contains("Hurdle");

            if (isObstacle)
            {
                HandleObstacleHit(col.gameObject);
                break;
            }
        }
    }

    public void HandleObstacleHit(GameObject obstacle)
    {
        if (GameManager.Instance != null && GameManager.Instance.isGameOver) return;
        if (isInvulnerable) return;

        // If hoverboard is active: BOTH hoverboard and obstacle are vanished, player keeps running forward!
        if (isHoverboardActive)
        {
            CrashHoverboard(obstacle);
            return;
        }

        // Without hoverboard: Game Over!
        forwardSpeed = 0f;
        AudioManager.Instance?.PlayCrash();
        if (GameManager.Instance != null)
        {
            GameManager.Instance.GameOver();
        }
    }

    public void CrashHoverboard(GameObject obstacle = null)
    {
        if (!isHoverboardActive) return;

        DeactivateHoverboard();
        isInvulnerable = true;
        invulnerableTimer = 1.6f;
        AudioManager.Instance?.PlayShieldSave();
        ShowBanner("💥 HOVERBOARD SHIELD SAVED YOU!");

        // 1. Camera impact shake
        if (CameraFollow.Instance != null)
        {
            CameraFollow.Instance.Shake(0.22f, 0.35f);
        }

        // 2. Vanish and explode the obstacle that was hit!
        if (obstacle != null)
        {
            GameObject obstacleRoot = obstacle;
            if (obstacle.transform.parent != null)
            {
                string pName = obstacle.transform.parent.name;
                if (pName.Contains("Train") || pName.Contains("Barrier") || pName.Contains("Hurdle") || obstacle.transform.parent.CompareTag("Obstacle"))
                {
                    obstacleRoot = obstacle.transform.parent.gameObject;
                }
            }
            ObstacleDemolishEffect.Demolish(obstacleRoot, transform.position + Vector3.forward * 0.8f + Vector3.up * 0.7f);
        }

        // PLAYER NEVER MOVES BACKWARD! Forward momentum continues seamlessly!
        forwardSpeed = currentBaseSpeed;
    }

    private void UpdateHoverboardState()
    {
        // Hoverboard countdown
        if (isHoverboardActive)
        {
            hoverboardTimer -= Time.deltaTime;
            if (hoverboardTimer <= 0f)
            {
                DeactivateHoverboard();
                ShowBanner("HOVERBOARD EXPIRED");
            }
        }

        // Invulnerability countdown & blinking flash
        if (isInvulnerable)
        {
            invulnerableTimer -= Time.deltaTime;
            bool flashVisible = (Mathf.FloorToInt(Time.time * 16f) % 2) == 0;
            SetRenderersVisible(flashVisible);

            if (invulnerableTimer <= 0f)
            {
                isInvulnerable = false;
                SetRenderersVisible(true);
            }
        }

        if (bannerTimer > 0f)
        {
            bannerTimer -= Time.deltaTime;
        }
    }

    private void AnimatePlayerAndBoard()
    {
        if (characterModel == null) return;

        if (isHoverboardActive)
        {
            // Hoverboard is active: Floating levitation bobbing
            float bob = Mathf.Sin(Time.time * 6f) * 0.035f;

            if (hoverboard != null)
            {
                hoverboard.localPosition = new Vector3(0f, -0.66f + bob, 0.04f);
                float pitch = Mathf.Sin(Time.time * 4f) * 2f;
                hoverboard.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }

            if (isSliding)
            {
                // Hoverboard low tuck: aerodynamic crouch low over deck to clear barriers
                float crouchProgress = Mathf.Sin(Mathf.Clamp01(1f - (slideTimer / slideDuration)) * Mathf.PI);
                Quaternion crouchRot = Quaternion.Euler(18f * crouchProgress, 30f, 0f);
                characterModel.localRotation = Quaternion.Lerp(characterModel.localRotation, crouchRot, 20f * Time.deltaTime);
                characterModel.localPosition = new Vector3(0f, -0.63f + bob - (0.28f * crouchProgress), -0.05f * crouchProgress);
                characterModel.localScale = new Vector3(baseScale, baseScale * (1f - 0.18f * crouchProgress), baseScale);
            }
            else
            {
                // Surfer stance: feet planted across the deck, body angled stylishly sideways (~30 deg)
                Quaternion surferStance = Quaternion.Euler(0f, 30f, 0f);
                characterModel.localRotation = Quaternion.Lerp(characterModel.localRotation, surferStance, 14f * Time.deltaTime);
                characterModel.localPosition = Vector3.Lerp(characterModel.localPosition, new Vector3(0f, -0.63f + bob, 0f), 14f * Time.deltaTime);
                characterModel.localScale = Vector3.one * baseScale;
            }
        }
        else
        {
            // Normal running on foot: sneakers touch the ground / rails directly!
            if (isSliding)
            {
                // Authentic Subway Surfers 360° acrobatic forward roll / somersault!
                float rollProgress = Mathf.Clamp01(1f - (slideTimer / slideDuration));
                float rollAngleRad = rollProgress * (Mathf.PI * 2f); // 0 to 2*PI (0° to 360°)
                float rollPitch = rollProgress * 360f;

                // 1. Full 360° forward pitch somersault
                characterModel.localRotation = Quaternion.Euler(rollPitch, 0f, 0f);

                // 2. Pivot compensation:
                // Because the 3D model's origin is at its feet (-0.75m), rotating around feet would dip the head underground.
                // By offsetting the local position by rollRadius * (1 - cos(theta)), the character rotates around their
                // hips/torso above the rails, guaranteeing the player NEVER dips below the track!
                float rollRadius = 0.40f;
                float yOffset = -0.75f + rollRadius * (1f - Mathf.Cos(rollAngleRad));
                float zOffset = rollRadius * Mathf.Sin(rollAngleRad);
                characterModel.localPosition = new Vector3(0f, yOffset, zOffset);

                // 3. Athletic tuck & squash into a compact rolling ball
                float curlTuck = Mathf.Sin(rollProgress * Mathf.PI);
                float squashX = baseScale * (1f + 0.08f * curlTuck);
                float squashY = baseScale * (1f - 0.20f * curlTuck);
                float squashZ = baseScale * (1f - 0.12f * curlTuck);
                characterModel.localScale = new Vector3(squashX, squashY, squashZ);
            }
            else
            {
                // Normal running upright posture directly on rails
                characterModel.localRotation = Quaternion.Lerp(characterModel.localRotation, Quaternion.identity, 16f * Time.deltaTime);
                characterModel.localPosition = Vector3.Lerp(characterModel.localPosition, new Vector3(0f, -0.75f, 0f), 16f * Time.deltaTime);
                characterModel.localScale = Vector3.one * baseScale;
            }
        }
    }

    public void CollectHoverboardItem()
    {
        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.AddHoverboard(1);
            hoverboardCount = ShopManager.Instance.GetHoverboardStock();
        }
        else
        {
            hoverboardCount++;
        }
        AudioManager.Instance?.PlayPowerup();
        ShowBanner(string.Format("🛹 +1 HOVERBOARD STORED! (Total: {0})", hoverboardCount));
    }

    public void CollectHoverboardPowerup(float duration = 15f)
    {
        CollectHoverboardItem();
    }

    public void ActivateHoverboard(float duration = 15f)
    {
        isHoverboardActive = true;
        hoverboardTimer = duration;
        forwardSpeed = currentBaseSpeed + hoverboardBonusSpeed;

        if (hoverboard == null) hoverboard = transform.Find("Hoverboard");
        if (hoverboard != null)
        {
            hoverboard.gameObject.SetActive(true);
            if (ShopManager.Instance != null)
            {
                var skin = ShopManager.Instance.GetEquippedSkin();
                Renderer[] rends = hoverboard.GetComponentsInChildren<Renderer>();
                for (int i = 0; i < rends.Length; i++)
                {
                    if (rends[i] != null && rends[i].material != null)
                    {
                        rends[i].material.color = skin.primaryColor;
                    }
                }
            }
        }
        if (animator != null)
        {
            animator.SetBool("IsHovering", true);
        }
    }

    public void DeactivateHoverboard()
    {
        isHoverboardActive = false;
        hoverboardTimer = 0f;
        forwardSpeed = currentBaseSpeed;

        if (hoverboard != null)
        {
            hoverboard.gameObject.SetActive(false);
        }
        if (animator != null)
        {
            animator.SetBool("IsHovering", false);
        }
    }

    public bool HasActiveHoverboard()
    {
        return isHoverboardActive;
    }

    private void SetRenderersVisible(bool visible)
    {
        if (characterRenderers == null) return;
        for (int i = 0; i < characterRenderers.Length; i++)
        {
            if (characterRenderers[i] != null) characterRenderers[i].enabled = visible;
        }
    }

    public void StartSlide()
    {
        isSliding = true;
        slideTimer = slideDuration;
        controller.height = slideHeight;
        controller.center = slideCenter;
    }

    public void StopSlide()
    {
        isSliding = false;
        controller.height = normalHeight;
        controller.center = normalCenter;
        if (characterModel != null)
        {
            characterModel.localPosition = new Vector3(0f, -0.75f, 0f);
            characterModel.localRotation = Quaternion.identity;
            characterModel.localScale = Vector3.one * baseScale;
        }
    }

    public void ShowBanner(string msg)
    {
        bannerMessage = msg;
        bannerTimer = 2.5f;
    }

    void OnGUI()
    {
        if (GameManager.Instance != null && (!GameManager.Instance.IsPlaying || GameManager.Instance.isGameOver || GameManager.Instance.isPaused)) return;

        // Center Notification Banner (e.g., "+1 Hoverboard Added", "Shield Absorbed Crash!")
        if (bannerTimer > 0f)
        {
            float sw = Screen.width;
            float sh = Screen.height;
            float scale = Mathf.Clamp(sw / 400f, 0.80f, 1.35f);

            float bW = Mathf.Clamp(sw * 0.75f, 260f, 440f);
            float bH = 38f * scale;
            Rect bRect = new Rect((sw - bW) / 2f, Mathf.Max(18f, sh * 0.035f) + (72f * scale), bW, bH);

            SubwayUI.DrawCard(bRect, SubwayUI.DarkCard, SubwayUI.GoldBorder, 1.5f);
            SubwayUI.DrawExtrudedText(bRect, bannerMessage, Mathf.RoundToInt(14 * scale),
                isHoverboardActive ? Color.cyan : Color.yellow, new Color(0f, 0f, 0f, 0.8f), 2);
        }
    }
}
