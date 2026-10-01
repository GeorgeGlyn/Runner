using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public enum GameState
    {
        StartScreen,
        Playing,
        Paused,
        GameOver
    }

    [Header("Game State")]
    public GameState gameState = GameState.StartScreen;
    public bool isGameOver => gameState == GameState.GameOver;
    public bool IsPlaying => gameState == GameState.Playing;
    public bool isPaused => gameState == GameState.Paused;

    [Header("Live Run Stats")]
    public float score = 0f;
    public int coins = 0;
    public bool isNewHighScore = false;

    [Header("Modals & Overlays")]
    public bool isSettingsOpen = false;
    public bool isStatsOpen = false;
    private bool confirmResetHighScore = false;

    private Transform player;
    private PlayerController playerController;
    private float initialHighScore = 0f;

    void Awake()
    {
        Instance = this;
        // Lock mobile device orientation to Portrait (Subway Surfers vertical standard)
        Screen.orientation = ScreenOrientation.Portrait;

        if (PlayerPrefs.GetInt("AutoStartRun", 0) == 1)
        {
            PlayerPrefs.SetInt("AutoStartRun", 0);
            PlayerPrefs.Save();
            gameState = GameState.Playing;
            Time.timeScale = 1f;
        }
        else
        {
            gameState = GameState.StartScreen;
            Time.timeScale = 1f;
        }

        initialHighScore = PlayerPrefs.GetFloat("HighScore", 0f);
    }

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            player = p.transform;
            playerController = p.GetComponent<PlayerController>();
        }

        if (gameState == GameState.Playing)
        {
            AudioManager.Instance?.PlayWhistle();
            RecordRunStarted();
        }
    }

    void Update()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
            {
                player = p.transform;
                playerController = p.GetComponent<PlayerController>();
            }
        }

        if (gameState == GameState.Playing && player != null)
        {
            score = player.position.z;

            if (initialHighScore > 0 && score > initialHighScore && !isNewHighScore)
            {
                isNewHighScore = true;
                AudioManager.Instance?.PlayHighScoreCelebration();
            }
        }
    }

    private void RecordRunStarted()
    {
        int runs = PlayerPrefs.GetInt("TotalRuns", 0) + 1;
        PlayerPrefs.SetInt("TotalRuns", runs);
        PlayerPrefs.Save();
    }

    // ── Game Flow Controls ───────────────────────────────────────────────────

    public void StartGame()
    {
        if (gameState != GameState.StartScreen) return;
        gameState = GameState.Playing;
        Time.timeScale = 1f;
        AudioManager.Instance?.PlayWhistle();
        RecordRunStarted();
    }

    public void PauseGame()
    {
        if (gameState != GameState.Playing) return;
        gameState = GameState.Paused;
        Time.timeScale = 0f;
        AudioManager.Instance?.PauseBGM();
        AudioManager.Instance?.PlayClick();
    }

    public void ResumeGame()
    {
        if (gameState != GameState.Paused) return;
        gameState = GameState.Playing;
        Time.timeScale = 1f;
        AudioManager.Instance?.PlayBGM();
        AudioManager.Instance?.PlayClick();
    }

    public void TogglePause()
    {
        if (gameState == GameState.Playing) PauseGame();
        else if (gameState == GameState.Paused) ResumeGame();
    }

    public void AddCoin()
    {
        coins++;
        ShopManager.Instance?.AddCoins(1);
    }

    public void GameOver()
    {
        if (gameState == GameState.GameOver) return;
        gameState = GameState.GameOver;
        Time.timeScale = 1f;

        float highScore = PlayerPrefs.GetFloat("HighScore", 0f);
        if (score > highScore)
        {
            isNewHighScore = true;
            highScore = score;
            PlayerPrefs.SetFloat("HighScore", highScore);
            AudioManager.Instance?.PlayHighScoreCelebration();
        }

        float totalDist = PlayerPrefs.GetFloat("TotalDistance", 0f) + score;
        PlayerPrefs.SetFloat("TotalDistance", totalDist);
        PlayerPrefs.Save();
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        PlayerPrefs.SetInt("AutoStartRun", 1);
        PlayerPrefs.Save();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToStartScreen()
    {
        Time.timeScale = 1f;
        PlayerPrefs.SetInt("AutoStartRun", 0);
        PlayerPrefs.Save();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // ── Mobile Responsive Dimensions Helper ──────────────────────────────────
    private float GetScale()
    {
        return Mathf.Clamp(Screen.width / 400f, 0.80f, 1.35f);
    }

    // ── OnGUI Entry ──────────────────────────────────────────────────────────

    void OnGUI()
    {
        if (ShopManager.Instance != null && ShopManager.Instance.isShopOpen)
        {
            return;
        }

        if (isSettingsOpen)
        {
            DrawSettingsScreen();
            return;
        }

        if (isStatsOpen)
        {
            DrawStatsScreen();
            return;
        }

        switch (gameState)
        {
            case GameState.StartScreen:
                DrawStartScreen();
                break;

            case GameState.Playing:
                DrawInGameHUD();
                break;

            case GameState.Paused:
                DrawInGameHUD();
                DrawPauseScreen();
                break;

            case GameState.GameOver:
                DrawGameOverScreen();
                break;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 1. MOBILE START SCREEN ("TAP TO PLAY")
    // ─────────────────────────────────────────────────────────────────────────
    private void DrawStartScreen()
    {
        float sw = Screen.width;
        float sh = Screen.height;
        float scale = GetScale();

        // Subtle gradient vignette top & bottom
        SubwayUI.DrawRect(new Rect(0, 0, sw, sh * 0.24f), new Color(0.04f, 0.06f, 0.10f, 0.45f));
        SubwayUI.DrawRect(new Rect(0, sh * 0.65f, sw, sh * 0.35f), new Color(0.04f, 0.06f, 0.10f, 0.60f));

        // ── Top Safe Area Badges ──
        float topY = Mathf.Max(18f, sh * 0.035f);
        float badgeH = 40f * scale;

        int highVal = Mathf.FloorToInt(PlayerPrefs.GetFloat("HighScore", 0f));
        int totalCoins = ShopManager.Instance != null ? ShopManager.Instance.GetTotalCoins() : 0;

        // High Score Trophy Pill (Top Left)
        float hsW = Mathf.Clamp(sw * 0.42f, 140f, 210f);
        Rect hsRect = new Rect(16f, topY, hsW, badgeH);
        SubwayUI.DrawPillBadge(hsRect, "🏆 BEST", highVal.ToString("N0"), SubwayUI.GoldBorder, Color.yellow, Mathf.RoundToInt(15 * scale));

        // Bank Coins Pill (Top Right)
        float coinW = Mathf.Clamp(sw * 0.36f, 120f, 170f);
        float settSize = badgeH;
        Rect coinRect = new Rect(sw - coinW - settSize - 24f, topY, coinW, badgeH);
        SubwayUI.DrawPillBadge(coinRect, "🪙", totalCoins.ToString("N0"), SubwayUI.GoldBorder, Color.yellow, Mathf.RoundToInt(15 * scale));

        // Settings Squircle Button (Far Top Right)
        Rect settRect = new Rect(sw - settSize - 16f, topY, settSize, settSize);
        if (SubwayUI.DrawChunkyButton(settRect, "⚙️", SubwayUI.BlueFace, SubwayUI.BlueBevel, Color.white, Mathf.RoundToInt(18 * scale)))
        {
            AudioManager.Instance?.PlayClick();
            isSettingsOpen = true;
            return;
        }

        // ── Subway Surfers 3D Graffiti Logo ──
        float logoY = topY + badgeH + (20f * scale);
        int titleFontSize = Mathf.RoundToInt(Mathf.Clamp(sh * 0.055f, 34f, 52f));
        SubwayUI.DrawExtrudedText(new Rect(0, logoY, sw, 55f * scale), "SUBWAY RUNNER", titleFontSize,
            new Color(1f, 0.85f, 0.10f), new Color(0.40f, 0.10f, 0.02f, 0.95f), 4);

        int subFontSize = Mathf.RoundToInt(Mathf.Clamp(sh * 0.022f, 13f, 18f));
        SubwayUI.DrawExtrudedText(new Rect(0, logoY + (46f * scale), sw, 28f * scale), "★ 3D RAILWAY SURF ★", subFontSize,
            Color.cyan, new Color(0f, 0f, 0f, 0.85f), 2);

        // ── Pulsing "TAP TO PLAY" (Lower Middle) ──
        float promptY = sh * 0.73f;
        float pulse = 0.65f + 0.35f * Mathf.Sin(Time.unscaledTime * 4.5f);

        int promptFontSize = Mathf.RoundToInt(Mathf.Clamp(sh * 0.040f, 26f, 40f));
        Color promptFace = new Color(1f, 0.90f, 0.18f, pulse);
        SubwayUI.DrawExtrudedText(new Rect(0, promptY, sw, 50f * scale), "▶ TAP TO PLAY ◀", promptFontSize,
            promptFace, new Color(0f, 0f, 0f, 0.85f * pulse), 3);

        GUIStyle hintStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(13 * scale),
            alignment = TextAnchor.MiddleCenter
        };
        hintStyle.normal.textColor = new Color(1f, 1f, 1f, 0.75f);
        GUI.Label(new Rect(0, promptY + (46f * scale), sw, 25f), "[ TAP SCREEN  •  SWIPE UP  •  SPACEBAR ]", hintStyle);

        // ── Bottom Mobile Thumb Action Bar ──
        float botY = sh - (64f * scale) - 16f;
        float btnW = Mathf.Clamp(sw * 0.38f, 130f, 180f);
        float btnH = 54f * scale;

        // SHOP / ME Button (Bottom Left)
        int boards = playerController != null ? playerController.hoverboardCount : ShopManager.Instance?.GetHoverboardStock() ?? 2;
        Rect shopBtnRect = new Rect(20f, botY, btnW, btnH);
        string shopTxt = string.Format("🛹 SHOP ({0})", boards);
        if (SubwayUI.DrawChunkyButton(shopBtnRect, shopTxt, SubwayUI.YellowFace, SubwayUI.YellowBevel, Color.black, Mathf.RoundToInt(15 * scale)))
        {
            AudioManager.Instance?.PlayClick();
            ShopManager.Instance?.OpenShop();
            return;
        }

        // STATS Button (Bottom Right)
        Rect statsBtnRect = new Rect(sw - btnW - 20f, botY, btnW, btnH);
        if (SubwayUI.DrawChunkyButton(statsBtnRect, "📊 STATS", SubwayUI.PurpleFace, SubwayUI.PurpleBevel, Color.white, Mathf.RoundToInt(15 * scale)))
        {
            AudioManager.Instance?.PlayClick();
            isStatsOpen = true;
            return;
        }

        // Full Screen Tap Detection (Outside top and bottom buttons)
        Event e = Event.current;
        if (e.type == EventType.MouseDown && e.mousePosition.y > (topY + badgeH + 20f) && e.mousePosition.y < botY)
        {
            StartGame();
            e.Use();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 2. MOBILE IN-GAME HUD (SUBWAY SURFERS TOP-RIGHT SIGNATURE STACK)
    // ─────────────────────────────────────────────────────────────────────────
    private void DrawInGameHUD()
    {
        float sw = Screen.width;
        float sh = Screen.height;
        float scale = GetScale();
        float topY = Mathf.Max(18f, sh * 0.035f);

        // ── Top Left: Pause Button ──
        float pauseSize = 42f * scale;
        Rect pauseRect = new Rect(16f, topY, pauseSize, pauseSize);
        if (SubwayUI.DrawChunkyButton(pauseRect, "⏸", SubwayUI.BlueFace, SubwayUI.BlueBevel, Color.white, Mathf.RoundToInt(18 * scale)))
        {
            TogglePause();
        }

        // ── Top Right: Score & Multiplier (Subway Surfers Signature Stack) ──
        float hudRightX = sw - 16f;

        // Distance Score
        int scoreVal = Mathf.FloorToInt(score);
        string scoreStr = scoreVal.ToString("D5"); // Subway Surfers leading zeros format: 00482
        int scoreFontSize = Mathf.RoundToInt(28 * scale);
        Rect scoreRect = new Rect(hudRightX - 220f, topY - 2f, 220f, 36f * scale);
        SubwayUI.DrawExtrudedText(scoreRect, scoreStr, scoreFontSize, Color.white, new Color(0f, 0f, 0f, 0.85f), 3, TextAnchor.MiddleRight);

        // Multiplier Pill Badge (Left of score)
        float multW = 44f * scale;
        float multH = 26f * scale;
        Rect multRect = new Rect(hudRightX - 220f - multW - 6f, topY + 4f, multW, multH);
        SubwayUI.DrawCard(multRect, SubwayUI.DarkCard, SubwayUI.GoldBorder, 1.5f);
        GUIStyle mStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(13 * scale),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        mStyle.normal.textColor = Color.yellow;
        GUI.Label(multRect, "x1", mStyle);

        // Coins Display (Directly below Score)
        float coinY = topY + (36f * scale);
        int coinFontSize = Mathf.RoundToInt(22 * scale);
        Rect coinRect = new Rect(hudRightX - 180f, coinY, 180f, 30f * scale);
        SubwayUI.DrawExtrudedText(coinRect, "🪙 " + coins, coinFontSize, Color.yellow, new Color(0.35f, 0.20f, 0f, 0.85f), 2, TextAnchor.MiddleRight);

        // ── High Score Live Notification Ribbon ──
        if (isNewHighScore)
        {
            float pulse = 0.7f + 0.3f * Mathf.Sin(Time.time * 6f);
            float ribbonW = Mathf.Clamp(sw * 0.60f, 200f, 300f);
            Rect nhsRect = new Rect((sw - ribbonW) / 2f, topY, ribbonW, 30f * scale);
            SubwayUI.DrawCard(nhsRect, new Color(0.1f, 0.08f, 0f, 0.85f), SubwayUI.GoldBorder, 1.5f);
            GUIStyle nhsStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(14 * scale),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            nhsStyle.normal.textColor = new Color(1f, 0.88f, 0.2f, pulse);
            GUI.Label(nhsRect, "★ NEW RECORD! ★", nhsStyle);
        }

        // ── Bottom-Left / Center: Hoverboard Status Bar / Button ──
        if (playerController != null)
        {
            if (playerController.isHoverboardActive)
            {
                // Active Shield Countdown Bar
                float barW = Mathf.Clamp(sw * 0.70f, 250f, 400f);
                float barH = 34f * scale;
                Rect barRect = new Rect((sw - barW) / 2f, sh - barH - (20f * scale), barW, barH);
                float progress = Mathf.Clamp01(playerController.hoverboardTimer / 15f);
                SubwayUI.DrawProgressBar(barRect, progress, Color.cyan, SubwayUI.DarkCard, SubwayUI.CyanBorder);

                string shieldTxt = string.Format("🛹 SHIELD ACTIVE: {0:F1}s", playerController.hoverboardTimer);
                GUIStyle shStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = Mathf.RoundToInt(13 * scale),
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                shStyle.normal.textColor = Color.white;
                GUI.Label(barRect, shieldTxt, shStyle);
            }
            else if (playerController.hoverboardCount > 0)
            {
                // Hoverboard Deploy Button (Bottom-Left)
                float btnW = Mathf.Clamp(sw * 0.38f, 130f, 180f);
                float btnH = 46f * scale;
                Rect hbRect = new Rect(20f, sh - btnH - (20f * scale), btnW, btnH);
                string text = string.Format("🛹 SURF (x{0})", playerController.hoverboardCount);
                if (SubwayUI.DrawChunkyButton(hbRect, text, SubwayUI.YellowFace, SubwayUI.YellowBevel, Color.black, Mathf.RoundToInt(14 * scale)))
                {
                    playerController.DeployHoverboard();
                }
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 3. MOBILE PAUSE SCREEN
    // ─────────────────────────────────────────────────────────────────────────
    private void DrawPauseScreen()
    {
        float sw = Screen.width;
        float sh = Screen.height;
        float scale = GetScale();

        // Dark navy vignette backdrop
        SubwayUI.DrawRect(new Rect(0, 0, sw, sh), new Color(0.04f, 0.06f, 0.10f, 0.88f));

        float modalW = Mathf.Clamp(sw * 0.86f, 320f, 440f);
        float modalH = 430f * scale;
        float modalX = (sw - modalW) / 2f;
        float modalY = (sh - modalH) / 2f;

        SubwayUI.DrawCard(new Rect(modalX, modalY, modalW, modalH), SubwayUI.DarkCard, SubwayUI.CyanBorder, 3f);

        // Header Title
        SubwayUI.DrawExtrudedText(new Rect(modalX, modalY + (16f * scale), modalW, 46f * scale),
            "PAUSED", Mathf.RoundToInt(34 * scale), Color.white, new Color(0f, 0f, 0f, 0.85f), 3);

        // Live Run Stats Pill Box
        Rect statsBox = new Rect(modalX + 20f, modalY + (70f * scale), modalW - 40f, 95f * scale);
        SubwayUI.DrawCard(statsBox, SubwayUI.InnerCard, SubwayUI.GrayBorder, 1.5f);

        GUIStyle sStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(15 * scale),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };
        float statPad = 14f * scale;
        sStyle.normal.textColor = Color.white;
        GUI.Label(new Rect(statsBox.x + statPad, statsBox.y + (10f * scale), statsBox.width - (statPad * 2f), 24f),
            "Distance Ran: " + Mathf.FloorToInt(score) + " m", sStyle);

        sStyle.normal.textColor = Color.yellow;
        GUI.Label(new Rect(statsBox.x + statPad, statsBox.y + (36f * scale), statsBox.width - (statPad * 2f), 24f),
            "Run Coins: 🪙 " + coins, sStyle);

        int bankTotal = ShopManager.Instance != null ? ShopManager.Instance.GetTotalCoins() : 0;
        sStyle.normal.textColor = Color.cyan;
        GUI.Label(new Rect(statsBox.x + statPad, statsBox.y + (62f * scale), statsBox.width - (statPad * 2f), 24f),
            "Total Bank: 🪙 " + bankTotal.ToString("N0"), sStyle);

        // Chunky 3D Mobile Thumb Buttons (Stacked)
        float btnY = modalY + (182f * scale);
        float btnW = modalW - 40f;
        float btnH = 50f * scale;
        float btnX = modalX + 20f;
        float spacing = 12f * scale;

        // 1. RESUME (Big Green Candy Button)
        if (SubwayUI.DrawChunkyButton(new Rect(btnX, btnY, btnW, btnH), "▶ RESUME RUN",
            SubwayUI.GreenFace, SubwayUI.GreenBevel, Color.white, Mathf.RoundToInt(18 * scale)))
        {
            ResumeGame();
        }

        // 2. RESTART (Yellow Candy Button)
        if (SubwayUI.DrawChunkyButton(new Rect(btnX, btnY + btnH + spacing, btnW, btnH), "🔄 RESTART",
            SubwayUI.YellowFace, SubwayUI.YellowBevel, Color.black, Mathf.RoundToInt(16 * scale)))
        {
            RestartGame();
        }

        // 3. MAIN MENU (Blue Candy Button)
        if (SubwayUI.DrawChunkyButton(new Rect(btnX, btnY + (btnH + spacing) * 2f, btnW, btnH), "🏠 MAIN MENU",
            SubwayUI.BlueFace, SubwayUI.BlueBevel, Color.white, Mathf.RoundToInt(16 * scale)))
        {
            GoToStartScreen();
        }

        // Audio Toggle Row (Bottom)
        float audioY = btnY + (btnH + spacing) * 3f + (2f * scale);
        string muteTxt = (AudioManager.Instance != null && AudioManager.Instance.isMuted) ? "🔇 SOUND: OFF" : "🔊 SOUND: ON";
        if (SubwayUI.DrawChunkyButton(new Rect(btnX + (btnW - 140f * scale) / 2f, audioY, 140f * scale, 34f * scale),
            muteTxt, SubwayUI.InnerCard, SubwayUI.GrayBorder, Color.gray, Mathf.RoundToInt(12 * scale)))
        {
            AudioManager.Instance?.ToggleMute();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 4. MOBILE GAME OVER SCREEN
    // ─────────────────────────────────────────────────────────────────────────
    private void DrawGameOverScreen()
    {
        float sw = Screen.width;
        float sh = Screen.height;
        float scale = GetScale();

        SubwayUI.DrawRect(new Rect(0, 0, sw, sh), new Color(0.04f, 0.06f, 0.10f, 0.90f));

        float modalW = Mathf.Clamp(sw * 0.88f, 320f, 440f);
        float modalH = (isNewHighScore ? 470f : 430f) * scale;
        float modalX = (sw - modalW) / 2f;
        float modalY = (sh - modalH) / 2f;

        SubwayUI.DrawCard(new Rect(modalX, modalY, modalW, modalH), SubwayUI.DarkCard, SubwayUI.RedBorder, 3f);

        // GAME OVER Banner
        SubwayUI.DrawExtrudedText(new Rect(modalX, modalY + (16f * scale), modalW, 46f * scale),
            "GAME OVER", Mathf.RoundToInt(36 * scale), SubwayUI.RedFace, new Color(0.35f, 0f, 0f, 0.95f), 4);

        float contentY = modalY + (68f * scale);

        // New High Score Golden Badge
        if (isNewHighScore)
        {
            float pulse = 0.7f + 0.3f * Mathf.Sin(Time.unscaledTime * 6f);
            float badgeW = modalW - 40f;
            Rect nhsBadge = new Rect(modalX + 20f, contentY, badgeW, 34f * scale);
            SubwayUI.DrawCard(nhsBadge, new Color(0.20f, 0.15f, 0.02f, 0.95f), SubwayUI.GoldBorder, 2f);

            GUIStyle nhsStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(16 * scale),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            nhsStyle.normal.textColor = new Color(1f, 0.88f, 0.2f, pulse);
            GUI.Label(nhsBadge, "🎉 NEW HIGH SCORE! 🎉", nhsStyle);
            contentY += 42f * scale;
        }

        // Hero Score Breakdown Card
        Rect statsBox = new Rect(modalX + 20f, contentY, modalW - 40f, 120f * scale);
        SubwayUI.DrawCard(statsBox, SubwayUI.InnerCard, SubwayUI.GrayBorder, 1.5f);

        float statPad = 16f * scale;
        GUIStyle sLabel = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(16 * scale),
            fontStyle = FontStyle.Bold
        };

        sLabel.normal.textColor = Color.white;
        GUI.Label(new Rect(statsBox.x + statPad, statsBox.y + (12f * scale), statsBox.width - (statPad * 2f), 24f),
            "Score: " + Mathf.FloorToInt(score) + " m", sLabel);

        sLabel.normal.textColor = new Color(1f, 0.85f, 0.2f);
        GUI.Label(new Rect(statsBox.x + statPad, statsBox.y + (38f * scale), statsBox.width - (statPad * 2f), 24f),
            "High Score: " + Mathf.FloorToInt(PlayerPrefs.GetFloat("HighScore", 0f)) + " m", sLabel);

        sLabel.normal.textColor = Color.yellow;
        GUI.Label(new Rect(statsBox.x + statPad, statsBox.y + (64f * scale), statsBox.width - (statPad * 2f), 24f),
            "Coins Collected: +🪙 " + coins, sLabel);

        int bankTotal = ShopManager.Instance != null ? ShopManager.Instance.GetTotalCoins() : 0;
        sLabel.normal.textColor = Color.cyan;
        GUI.Label(new Rect(statsBox.x + statPad, statsBox.y + (90f * scale), statsBox.width - (statPad * 2f), 24f),
            "Total Bank: 🪙 " + bankTotal.ToString("N0"), sLabel);

        // Buttons
        float btnY = statsBox.y + statsBox.height + (18f * scale);
        float btnW = modalW - 40f;
        float btnX = modalX + 20f;

        // 1. PLAY AGAIN (Big Green Primary Candy Button)
        if (SubwayUI.DrawChunkyButton(new Rect(btnX, btnY, btnW, 56f * scale), "▶ PLAY AGAIN",
            SubwayUI.GreenFace, SubwayUI.GreenBevel, Color.white, Mathf.RoundToInt(20 * scale)))
        {
            RestartGame();
        }

        // 2. Secondary Row: MENU and SHOP
        float subY = btnY + (64f * scale);
        float halfW = (btnW / 2f) - 6f;

        if (SubwayUI.DrawChunkyButton(new Rect(btnX, subY, halfW, 46f * scale), "🏠 MENU",
            SubwayUI.BlueFace, SubwayUI.BlueBevel, Color.white, Mathf.RoundToInt(16 * scale)))
        {
            GoToStartScreen();
        }

        if (SubwayUI.DrawChunkyButton(new Rect(btnX + halfW + 12f, subY, halfW, 46f * scale), "🛹 SHOP",
            SubwayUI.YellowFace, SubwayUI.YellowBevel, Color.black, Mathf.RoundToInt(16 * scale)))
        {
            ShopManager.Instance?.OpenShop();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 5. MOBILE SETTINGS SCREEN
    // ─────────────────────────────────────────────────────────────────────────
    private void DrawSettingsScreen()
    {
        float sw = Screen.width;
        float sh = Screen.height;
        float scale = GetScale();

        SubwayUI.DrawRect(new Rect(0, 0, sw, sh), new Color(0.04f, 0.06f, 0.10f, 0.90f));

        float modalW = Mathf.Clamp(sw * 0.88f, 320f, 440f);
        float modalH = 430f * scale;
        float modalX = (sw - modalW) / 2f;
        float modalY = (sh - modalH) / 2f;

        SubwayUI.DrawCard(new Rect(modalX, modalY, modalW, modalH), SubwayUI.DarkCard, SubwayUI.CyanBorder, 3f);

        SubwayUI.DrawExtrudedText(new Rect(modalX, modalY + (16f * scale), modalW, 40f * scale),
            "SETTINGS", Mathf.RoundToInt(30 * scale), Color.cyan, new Color(0f, 0f, 0f, 0.85f), 3);

        float itemY = modalY + (68f * scale);
        float itemW = modalW - 40f;
        float itemX = modalX + 20f;

        // Sound Toggle
        bool isMuted = AudioManager.Instance != null && AudioManager.Instance.isMuted;
        string sndTxt = isMuted ? "SOUND & MUSIC: [ OFF ]" : "SOUND & MUSIC: [ ON ]";
        Color sndFace = isMuted ? SubwayUI.RedFace : SubwayUI.GreenFace;
        Color sndBevel = isMuted ? SubwayUI.RedBevel : SubwayUI.GreenBevel;
        if (SubwayUI.DrawChunkyButton(new Rect(itemX, itemY, itemW, 46f * scale), sndTxt,
            sndFace, sndBevel, Color.white, Mathf.RoundToInt(15 * scale)))
        {
            AudioManager.Instance?.ToggleMute();
        }

        // Controls Cheatsheet
        itemY += 56f * scale;
        Rect ctrlBox = new Rect(itemX, itemY, itemW, 115f * scale);
        SubwayUI.DrawCard(ctrlBox, SubwayUI.InnerCard, SubwayUI.GrayBorder, 1.5f);

        float pad = 12f * scale;
        GUIStyle cHeader = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(14 * scale), fontStyle = FontStyle.Bold };
        cHeader.normal.textColor = Color.yellow;
        GUI.Label(new Rect(ctrlBox.x + pad, ctrlBox.y + (8f * scale), ctrlBox.width - (pad * 2f), 20f), "CONTROLS GUIDE", cHeader);

        GUIStyle cText = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(13 * scale) };
        cText.normal.textColor = Color.white;
        GUI.Label(new Rect(ctrlBox.x + pad, ctrlBox.y + (30f * scale), ctrlBox.width - (pad * 2f), 20f), "• Swipe / Arrow Keys: Move & Jump", cText);
        GUI.Label(new Rect(ctrlBox.x + pad, ctrlBox.y + (54f * scale), ctrlBox.width - (pad * 2f), 20f), "• Swipe Down / Down Key: Forward Roll", cText);
        GUI.Label(new Rect(ctrlBox.x + pad, ctrlBox.y + (78f * scale), ctrlBox.width - (pad * 2f), 20f), "• Double Tap / 'E': Deploy Hoverboard", cText);

        // Reset High Score Button
        itemY += 128f * scale;
        string rstText = confirmResetHighScore ? "CONFIRM: ARE YOU SURE?" : "RESET HIGH SCORE";
        if (SubwayUI.DrawChunkyButton(new Rect(itemX, itemY, itemW, 42f * scale), rstText,
            SubwayUI.InnerCard, SubwayUI.RedBevel, new Color(1f, 0.4f, 0.4f), Mathf.RoundToInt(14 * scale)))
        {
            if (confirmResetHighScore)
            {
                PlayerPrefs.SetFloat("HighScore", 0f);
                PlayerPrefs.Save();
                confirmResetHighScore = false;
                AudioManager.Instance?.PlayClick();
            }
            else
            {
                confirmResetHighScore = true;
            }
        }

        // Close Button
        itemY += 52f * scale;
        if (SubwayUI.DrawChunkyButton(new Rect(itemX, itemY, itemW, 46f * scale), "✕ CLOSE SETTINGS",
            SubwayUI.BlueFace, SubwayUI.BlueBevel, Color.white, Mathf.RoundToInt(16 * scale)))
        {
            confirmResetHighScore = false;
            isSettingsOpen = false;
            AudioManager.Instance?.PlayClick();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 6. MOBILE STATS SCREEN
    // ─────────────────────────────────────────────────────────────────────────
    private void DrawStatsScreen()
    {
        float sw = Screen.width;
        float sh = Screen.height;
        float scale = GetScale();

        SubwayUI.DrawRect(new Rect(0, 0, sw, sh), new Color(0.04f, 0.06f, 0.10f, 0.90f));

        float modalW = Mathf.Clamp(sw * 0.88f, 320f, 440f);
        float modalH = 410f * scale;
        float modalX = (sw - modalW) / 2f;
        float modalY = (sh - modalH) / 2f;

        SubwayUI.DrawCard(new Rect(modalX, modalY, modalW, modalH), SubwayUI.DarkCard, SubwayUI.PurpleFace, 3f);

        SubwayUI.DrawExtrudedText(new Rect(modalX, modalY + (16f * scale), modalW, 40f * scale),
            "RUNNER STATS", Mathf.RoundToInt(30 * scale), new Color(0.85f, 0.65f, 1f), new Color(0f, 0f, 0f, 0.85f), 3);

        Rect sBox = new Rect(modalX + 20f, modalY + (68f * scale), modalW - 40f, 240f * scale);
        SubwayUI.DrawCard(sBox, SubwayUI.InnerCard, SubwayUI.GrayBorder, 1.5f);

        int bestScore = Mathf.FloorToInt(PlayerPrefs.GetFloat("HighScore", 0f));
        int totalRuns = PlayerPrefs.GetInt("TotalRuns", 0);
        float totalDist = PlayerPrefs.GetFloat("TotalDistance", 0f);
        int totalCoins = ShopManager.Instance != null ? ShopManager.Instance.GetTotalCoins() : 0;
        int boardsStock = ShopManager.Instance != null ? ShopManager.Instance.GetHoverboardStock() : 0;

        float pad = 16f * scale;
        GUIStyle lineStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(15 * scale),
            fontStyle = FontStyle.Bold
        };

        lineStyle.normal.textColor = Color.yellow;
        GUI.Label(new Rect(sBox.x + pad, sBox.y + (16f * scale), sBox.width - (pad * 2f), 24f), "🏆 Best Distance: " + bestScore + " m", lineStyle);

        lineStyle.normal.textColor = Color.white;
        GUI.Label(new Rect(sBox.x + pad, sBox.y + (54f * scale), sBox.width - (pad * 2f), 24f), "🏃 Total Runs: " + totalRuns, lineStyle);

        lineStyle.normal.textColor = Color.cyan;
        GUI.Label(new Rect(sBox.x + pad, sBox.y + (92f * scale), sBox.width - (pad * 2f), 24f), "📏 Lifetime Distance: " + Mathf.FloorToInt(totalDist) + " m", lineStyle);

        lineStyle.normal.textColor = Color.yellow;
        GUI.Label(new Rect(sBox.x + pad, sBox.y + (130f * scale), sBox.width - (pad * 2f), 24f), "🪙 Total Bank Coins: " + totalCoins.ToString("N0"), lineStyle);

        lineStyle.normal.textColor = new Color(0.3f, 0.9f, 0.5f);
        GUI.Label(new Rect(sBox.x + pad, sBox.y + (168f * scale), sBox.width - (pad * 2f), 24f), "🛹 Hoverboards in Stock: " + boardsStock, lineStyle);

        // Close Button
        if (SubwayUI.DrawChunkyButton(new Rect(modalX + 20f, modalY + modalH - (58f * scale), modalW - 40f, 48f * scale),
            "✕ CLOSE STATS", SubwayUI.PurpleFace, SubwayUI.PurpleBevel, Color.white, Mathf.RoundToInt(16 * scale)))
        {
            isStatsOpen = false;
            AudioManager.Instance?.PlayClick();
        }
    }
}
