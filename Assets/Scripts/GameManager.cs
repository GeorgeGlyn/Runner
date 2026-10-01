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
    private float initialHighScore = 0f;

    // Drawing textures
    private Texture2D whiteTex;
    private Texture2D darkBoxTex;

    void Awake()
    {
        Instance = this;
        Screen.orientation = ScreenOrientation.Portrait;

        // AutoStartRun: Set to 1 when 'Play Again' is tapped from Game Over
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
        if (p != null) player = p.transform;

        if (gameState == GameState.Playing)
        {
            AudioManager.Instance?.PlayWhistle();
            RecordRunStarted();
        }
    }

    void Update()
    {
        if (gameState == GameState.Playing && player != null)
        {
            score = player.position.z;

            // Live high score beat detection
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

        // Lifetime stats
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

    // ── Texture & Style Helpers ──────────────────────────────────────────────

    private Texture2D GetWhiteTex()
    {
        if (whiteTex == null)
        {
            whiteTex = new Texture2D(1, 1);
            whiteTex.SetPixel(0, 0, Color.white);
            whiteTex.Apply();
        }
        return whiteTex;
    }

    private void DrawOverlay(Rect rect, Color color)
    {
        Color old = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, GetWhiteTex());
        GUI.color = old;
    }

    private void DrawCard(Rect rect, Color bgColor, Color borderColor, float border = 2f)
    {
        DrawOverlay(rect, borderColor);
        Rect inner = new Rect(rect.x + border, rect.y + border, rect.width - (border * 2f), rect.height - (border * 2f));
        DrawOverlay(inner, bgColor);
    }

    private void DrawShadowLabel(Rect rect, string text, GUIStyle style, Color textColor, Color shadowColor, float offset = 2f)
    {
        Color old = style.normal.textColor;
        style.normal.textColor = shadowColor;
        GUI.Label(new Rect(rect.x + offset, rect.y + offset, rect.width, rect.height), text, style);
        style.normal.textColor = textColor;
        GUI.Label(rect, text, style);
        style.normal.textColor = old;
    }

    // ── GUI Rendering ────────────────────────────────────────────────────────

    void OnGUI()
    {
        // If the Shop Modal is open, let ShopManager handle its full screen overlay
        if (ShopManager.Instance != null && ShopManager.Instance.isShopOpen)
        {
            return;
        }

        // Settings Modal
        if (isSettingsOpen)
        {
            DrawSettingsScreen();
            return;
        }

        // Stats Modal
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
    // 1. INTRO / START SCREEN ("TAP TO PLAY")
    // ─────────────────────────────────────────────────────────────────────────
    private void DrawStartScreen()
    {
        float sw = Screen.width;
        float sh = Screen.height;

        // Subtle gradient vignette on top and bottom
        DrawOverlay(new Rect(0, 0, sw, sh * 0.22f), new Color(0f, 0f, 0f, 0.40f));
        DrawOverlay(new Rect(0, sh * 0.70f, sw, sh * 0.30f), new Color(0f, 0f, 0f, 0.55f));

        // ── Top Header Badges ──
        float topY = 20f;
        int highVal = Mathf.FloorToInt(PlayerPrefs.GetFloat("HighScore", 0f));
        int totalCoins = ShopManager.Instance != null ? ShopManager.Instance.GetTotalCoins() : 0;

        // High Score Badge (Top Left)
        Rect hsRect = new Rect(20f, topY, Mathf.Clamp(sw * 0.38f, 150f, 220f), 38f);
        DrawCard(hsRect, new Color(0.12f, 0.14f, 0.18f, 0.88f), new Color(1f, 0.84f, 0f, 0.8f));
        GUIStyle hsStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.Clamp(Mathf.RoundToInt(sh * 0.020f), 13, 17),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        hsStyle.normal.textColor = new Color(1f, 0.88f, 0.2f);
        GUI.Label(hsRect, "🏆 BEST: " + highVal, hsStyle);

        // Bank Coins Badge (Top Right)
        float coinW = Mathf.Clamp(sw * 0.32f, 130f, 180f);
        Rect coinRect = new Rect(sw - coinW - 20f, topY, coinW, 38f);
        DrawCard(coinRect, new Color(0.12f, 0.14f, 0.18f, 0.88f), new Color(1f, 0.84f, 0f, 0.8f));
        GUIStyle coinStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.Clamp(Mathf.RoundToInt(sh * 0.020f), 13, 17),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        coinStyle.normal.textColor = Color.yellow;
        GUI.Label(coinRect, "🪙 " + totalCoins, coinStyle);

        // ── Quick Action Buttons Row (Below Top Badges) ──
        float btnRowY = topY + 48f;
        float btnH = 34f;
        float btnW = 58f;
        float spacing = 8f;
        float totalRowW = (btnW * 4f) + (spacing * 3f);
        float startBtnX = (sw - totalRowW) / 2f;

        GUIStyle subBtnStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold
        };

        Rect shopBtnRect = new Rect(startBtnX, btnRowY, btnW, btnH);
        Rect settBtnRect = new Rect(startBtnX + btnW + spacing, btnRowY, btnW, btnH);
        Rect statsBtnRect = new Rect(startBtnX + (btnW + spacing) * 2f, btnRowY, btnW, btnH);
        Rect muteBtnRect = new Rect(startBtnX + (btnW + spacing) * 3f, btnRowY, btnH, btnH);

        // SHOP
        subBtnStyle.normal.textColor = Color.yellow;
        if (GUI.Button(shopBtnRect, "🛹 SHOP", subBtnStyle))
        {
            AudioManager.Instance?.PlayClick();
            ShopManager.Instance?.OpenShop();
            return;
        }

        // SETTINGS
        subBtnStyle.normal.textColor = Color.cyan;
        if (GUI.Button(settBtnRect, "⚙️ SET", subBtnStyle))
        {
            AudioManager.Instance?.PlayClick();
            isSettingsOpen = true;
            return;
        }

        // STATS
        subBtnStyle.normal.textColor = new Color(0.3f, 0.9f, 0.4f);
        if (GUI.Button(statsBtnRect, "📊 STAT", subBtnStyle))
        {
            AudioManager.Instance?.PlayClick();
            isStatsOpen = true;
            return;
        }

        // AUDIO TOGGLE
        subBtnStyle.normal.textColor = Color.white;
        string muteIcon = (AudioManager.Instance != null && AudioManager.Instance.isMuted) ? "🔇" : "🔊";
        if (GUI.Button(muteBtnRect, muteIcon, subBtnStyle))
        {
            AudioManager.Instance?.ToggleMute();
            return;
        }

        // ── Title Logo (Subway Surfers Graffiti Gold Styling) ──
        float titleY = sh * 0.17f;
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.Clamp(Mathf.RoundToInt(sh * 0.052f), 32, 54),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        DrawShadowLabel(new Rect(0, titleY, sw, 55f), "SUBWAY RUNNER", titleStyle,
            new Color(1f, 0.85f, 0.15f), new Color(0.6f, 0.15f, 0f, 0.9f), 3f);

        GUIStyle subTitleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.Clamp(Mathf.RoundToInt(sh * 0.022f), 13, 19),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        DrawShadowLabel(new Rect(0, titleY + 46f, sw, 30f), "★ 3D RAILWAY ADVENTURE ★", subTitleStyle,
            Color.cyan, new Color(0f, 0f, 0f, 0.8f), 2f);

        // ── Pulsing "TAP TO RUN" Prompt (Center/Bottom) ──
        float promptY = sh * 0.77f;
        float pulse = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 5.0f);

        GUIStyle promptStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.Clamp(Mathf.RoundToInt(sh * 0.038f), 24, 38),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        Color promptCol = new Color(1f, 0.90f, 0.20f, pulse);
        DrawShadowLabel(new Rect(0, promptY, sw, 48f), "▶ TAP TO RUN ◀", promptStyle,
            promptCol, new Color(0f, 0f, 0f, 0.85f * pulse), 2.5f);

        GUIStyle keyStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.Clamp(Mathf.RoundToInt(sh * 0.018f), 12, 16),
            alignment = TextAnchor.MiddleCenter
        };
        keyStyle.normal.textColor = new Color(1f, 1f, 1f, 0.80f);
        GUI.Label(new Rect(0, promptY + 44f, sw, 25f), "[ TAP SCREEN  •  SWIPE UP  •  SPACEBAR ]", keyStyle);

        // ── Global Tap-to-Start Detection ──
        // If user clicks anywhere on screen (outside top header buttons), start run!
        Event e = Event.current;
        if (e.type == EventType.MouseDown && e.mousePosition.y > (btnRowY + btnH + 10f))
        {
            StartGame();
            e.Use();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 2. IN-GAME HUD
    // ─────────────────────────────────────────────────────────────────────────
    private void DrawInGameHUD()
    {
        int baseFontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height * 0.030f), 18, 28);

        // Score (Top Left)
        GUIStyle scoreStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = baseFontSize,
            fontStyle = FontStyle.Bold
        };
        DrawShadowLabel(new Rect(25, 20, 260, 40), "Score: " + Mathf.FloorToInt(score), scoreStyle,
            Color.white, new Color(0f, 0f, 0f, 0.75f), 2f);

        // New High Score Banner notification during live run!
        if (isNewHighScore)
        {
            GUIStyle nhsStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height * 0.022f), 14, 20),
                fontStyle = FontStyle.Bold
            };
            float pulse = 0.6f + 0.4f * Mathf.Sin(Time.time * 6f);
            nhsStyle.normal.textColor = new Color(1f, 0.85f, 0.2f, pulse);
            GUI.Label(new Rect(25, 54, 260, 25), "★ NEW RECORD! ★", nhsStyle);
        }

        // Coins (Top Right)
        GUIStyle coinStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = baseFontSize,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleRight
        };
        DrawShadowLabel(new Rect(Screen.width - 250, 20, 160, 40), "🪙 " + coins, coinStyle,
            Color.yellow, new Color(0f, 0f, 0f, 0.75f), 2f);

        // Pause Button (Top Right HUD)
        GUIStyle pauseBtnStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold
        };
        if (GUI.Button(new Rect(Screen.width - 76, 20, 48, 36), "⏸", pauseBtnStyle))
        {
            TogglePause();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 3. PAUSE SCREEN
    // ─────────────────────────────────────────────────────────────────────────
    private void DrawPauseScreen()
    {
        float sw = Screen.width;
        float sh = Screen.height;

        // Dark dim backdrop
        DrawOverlay(new Rect(0, 0, sw, sh), new Color(0f, 0f, 0f, 0.72f));

        float modalW = Mathf.Clamp(sw * 0.80f, 320f, 440f);
        float modalH = 390f;
        float modalX = (sw - modalW) / 2f;
        float modalY = (sh - modalH) / 2f;

        DrawCard(new Rect(modalX, modalY, modalW, modalH),
            new Color(0.11f, 0.13f, 0.18f, 0.96f), new Color(0.2f, 0.7f, 1f, 0.85f), 3f);

        // Title
        GUIStyle pTitleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 34,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        DrawShadowLabel(new Rect(modalX, modalY + 18, modalW, 45), "GAME PAUSED", pTitleStyle,
            new Color(1f, 0.84f, 0f), new Color(0f, 0f, 0f, 0.85f), 2f);

        // Run Summary Box
        Rect statsBox = new Rect(modalX + 25f, modalY + 70f, modalW - 50f, 95f);
        DrawCard(statsBox, new Color(0.06f, 0.08f, 0.12f, 0.8f), new Color(0.3f, 0.4f, 0.5f, 0.5f), 1f);

        GUIStyle statLabelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 17,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };
        statLabelStyle.normal.textColor = Color.white;
        GUI.Label(new Rect(statsBox.x + 15, statsBox.y + 12, statsBox.width - 30, 24), "Current Distance: " + Mathf.FloorToInt(score) + " m", statLabelStyle);

        statLabelStyle.normal.textColor = Color.yellow;
        GUI.Label(new Rect(statsBox.x + 15, statsBox.y + 38, statsBox.width - 30, 24), "Run Coins: 🪙 " + coins, statLabelStyle);

        int bankTotal = ShopManager.Instance != null ? ShopManager.Instance.GetTotalCoins() : 0;
        statLabelStyle.normal.textColor = Color.cyan;
        GUI.Label(new Rect(statsBox.x + 15, statsBox.y + 64, statsBox.width - 30, 24), "Bank Total: 🪙 " + bankTotal, statLabelStyle);

        // ── Action Buttons ──
        float btnY = modalY + 182f;
        float btnW = modalW - 50f;
        float btnX = modalX + 25f;

        // 1. RESUME (Big Green)
        GUIStyle resumeStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 20,
            fontStyle = FontStyle.Bold
        };
        resumeStyle.normal.textColor = new Color(0.18f, 0.95f, 0.40f);
        if (GUI.Button(new Rect(btnX, btnY, btnW, 46f), "▶ RESUME RUN", resumeStyle))
        {
            ResumeGame();
        }

        // 2. RESTART (Yellow)
        GUIStyle restartStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 17,
            fontStyle = FontStyle.Bold
        };
        restartStyle.normal.textColor = new Color(1f, 0.85f, 0.2f);
        if (GUI.Button(new Rect(btnX, btnY + 54f, btnW, 42f), "🔄 RESTART", restartStyle))
        {
            RestartGame();
        }

        // 3. MAIN MENU (Cyan)
        GUIStyle menuStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold
        };
        menuStyle.normal.textColor = Color.cyan;
        if (GUI.Button(new Rect(btnX, btnY + 104f, btnW, 40f), "🏠 MAIN MENU", menuStyle))
        {
            GoToStartScreen();
        }

        // 4. Mute toggle row (Bottom)
        string muteTxt = (AudioManager.Instance != null && AudioManager.Instance.isMuted) ? "SOUND: OFF" : "SOUND: ON";
        GUIStyle sToggleStyle = new GUIStyle(GUI.skin.button) { fontSize = 12, fontStyle = FontStyle.Bold };
        sToggleStyle.normal.textColor = Color.gray;
        if (GUI.Button(new Rect(btnX + (btnW - 120f) / 2f, btnY + 152f, 120f, 28f), muteTxt, sToggleStyle))
        {
            AudioManager.Instance?.ToggleMute();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 4. GAME OVER SCREEN
    // ─────────────────────────────────────────────────────────────────────────
    private void DrawGameOverScreen()
    {
        float sw = Screen.width;
        float sh = Screen.height;

        DrawOverlay(new Rect(0, 0, sw, sh), new Color(0f, 0f, 0f, 0.78f));

        float modalW = Mathf.Clamp(sw * 0.82f, 320f, 440f);
        float modalH = isNewHighScore ? 430f : 390f;
        float modalX = (sw - modalW) / 2f;
        float modalY = (sh - modalH) / 2f;

        DrawCard(new Rect(modalX, modalY, modalW, modalH),
            new Color(0.12f, 0.14f, 0.18f, 0.97f), new Color(1f, 0.25f, 0.25f, 0.85f), 3f);

        // GAME OVER Banner
        GUIStyle goStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 38,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        DrawShadowLabel(new Rect(modalX, modalY + 16, modalW, 46), "GAME OVER", goStyle,
            new Color(1f, 0.25f, 0.25f), new Color(0.4f, 0f, 0f, 0.9f), 3f);

        float contentY = modalY + 68f;

        // New High Score Banner
        if (isNewHighScore)
        {
            GUIStyle nhsStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 19,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            float pulse = 0.7f + 0.3f * Mathf.Sin(Time.unscaledTime * 6f);
            nhsStyle.normal.textColor = new Color(1f, 0.88f, 0.2f, pulse);
            GUI.Label(new Rect(modalX, contentY, modalW, 28), "🎉 NEW HIGH SCORE! 🎉", nhsStyle);
            contentY += 34f;
        }

        // Stats Box
        Rect statsBox = new Rect(modalX + 25f, contentY, modalW - 50f, 105f);
        DrawCard(statsBox, new Color(0.06f, 0.08f, 0.12f, 0.85f), new Color(0.3f, 0.35f, 0.45f, 0.6f), 1f);

        GUIStyle sLabel = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold };
        sLabel.normal.textColor = Color.white;
        GUI.Label(new Rect(statsBox.x + 16, statsBox.y + 10, statsBox.width - 32, 22), "Score: " + Mathf.FloorToInt(score) + " m", sLabel);

        sLabel.normal.textColor = new Color(1f, 0.85f, 0.2f);
        GUI.Label(new Rect(statsBox.x + 16, statsBox.y + 34, statsBox.width - 32, 22), "High Score: " + Mathf.FloorToInt(PlayerPrefs.GetFloat("HighScore", 0f)) + " m", sLabel);

        sLabel.normal.textColor = Color.yellow;
        GUI.Label(new Rect(statsBox.x + 16, statsBox.y + 58, statsBox.width - 32, 22), "Coins Earned: +🪙 " + coins, sLabel);

        int bankTotal = ShopManager.Instance != null ? ShopManager.Instance.GetTotalCoins() : 0;
        sLabel.normal.textColor = Color.cyan;
        GUI.Label(new Rect(statsBox.x + 16, statsBox.y + 80, statsBox.width - 32, 22), "Total Bank: 🪙 " + bankTotal, sLabel);

        // Buttons
        float btnY = statsBox.y + statsBox.height + 16f;
        float btnW = modalW - 50f;
        float btnX = modalX + 25f;

        // 1. PLAY AGAIN (Big Green)
        GUIStyle paStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 22,
            fontStyle = FontStyle.Bold
        };
        paStyle.normal.textColor = new Color(0.18f, 0.95f, 0.40f);
        if (GUI.Button(new Rect(btnX, btnY, btnW, 52f), "▶ PLAY AGAIN", paStyle))
        {
            RestartGame();
        }

        // 2. MAIN MENU (Cyan)
        GUIStyle mmStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 17,
            fontStyle = FontStyle.Bold
        };
        mmStyle.normal.textColor = Color.cyan;
        if (GUI.Button(new Rect(btnX, btnY + 60f, (btnW / 2f) - 6f, 44f), "🏠 MENU", mmStyle))
        {
            GoToStartScreen();
        }

        // 3. SHOP (Yellow)
        GUIStyle spStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 17,
            fontStyle = FontStyle.Bold
        };
        spStyle.normal.textColor = Color.yellow;
        if (GUI.Button(new Rect(btnX + (btnW / 2f) + 6f, btnY + 60f, (btnW / 2f) - 6f, 44f), "🛹 SHOP", spStyle))
        {
            ShopManager.Instance?.OpenShop();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 5. SETTINGS SCREEN
    // ─────────────────────────────────────────────────────────────────────────
    private void DrawSettingsScreen()
    {
        float sw = Screen.width;
        float sh = Screen.height;

        DrawOverlay(new Rect(0, 0, sw, sh), new Color(0f, 0f, 0f, 0.78f));

        float modalW = Mathf.Clamp(sw * 0.82f, 320f, 440f);
        float modalH = 410f;
        float modalX = (sw - modalW) / 2f;
        float modalY = (sh - modalH) / 2f;

        DrawCard(new Rect(modalX, modalY, modalW, modalH),
            new Color(0.11f, 0.13f, 0.18f, 0.97f), new Color(0.2f, 0.7f, 1f, 0.85f), 3f);

        GUIStyle sTitle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 30,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        DrawShadowLabel(new Rect(modalX, modalY + 18, modalW, 40), "SETTINGS", sTitle,
            Color.cyan, new Color(0f, 0f, 0f, 0.8f), 2f);

        float itemY = modalY + 70f;
        float itemW = modalW - 50f;
        float itemX = modalX + 25f;

        // Sound Toggle
        bool isMuted = AudioManager.Instance != null && AudioManager.Instance.isMuted;
        string sndTxt = isMuted ? "SOUND & MUSIC: [ OFF ]" : "SOUND & MUSIC: [ ON ]";
        GUIStyle toggleBtnStyle = new GUIStyle(GUI.skin.button) { fontSize = 16, fontStyle = FontStyle.Bold };
        toggleBtnStyle.normal.textColor = isMuted ? Color.red : Color.green;
        if (GUI.Button(new Rect(itemX, itemY, itemW, 42f), sndTxt, toggleBtnStyle))
        {
            AudioManager.Instance?.ToggleMute();
        }

        // Controls Box
        itemY += 52f;
        Rect ctrlBox = new Rect(itemX, itemY, itemW, 110f);
        DrawCard(ctrlBox, new Color(0.06f, 0.08f, 0.12f, 0.85f), new Color(0.3f, 0.4f, 0.5f, 0.5f), 1f);

        GUIStyle cHeader = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold };
        cHeader.normal.textColor = Color.yellow;
        GUI.Label(new Rect(ctrlBox.x + 12, ctrlBox.y + 8, ctrlBox.width - 24, 20), "CONTROLS GUIDE", cHeader);

        GUIStyle cText = new GUIStyle(GUI.skin.label) { fontSize = 13 };
        cText.normal.textColor = Color.white;
        GUI.Label(new Rect(ctrlBox.x + 12, ctrlBox.y + 30, ctrlBox.width - 24, 20), "• Swipe / Arrow Keys: Move & Jump", cText);
        GUI.Label(new Rect(ctrlBox.x + 12, ctrlBox.y + 52, ctrlBox.width - 24, 20), "• Swipe Down / Down Key: Forward Roll", cText);
        GUI.Label(new Rect(ctrlBox.x + 12, ctrlBox.y + 74, ctrlBox.width - 24, 20), "• Double Tap / 'E': Deploy Hoverboard", cText);

        // Reset High Score Button
        itemY += 122f;
        GUIStyle rstStyle = new GUIStyle(GUI.skin.button) { fontSize = 14, fontStyle = FontStyle.Bold };
        rstStyle.normal.textColor = new Color(1f, 0.4f, 0.4f);
        string rstText = confirmResetHighScore ? "CONFIRM: ARE YOU SURE?" : "RESET HIGH SCORE";
        if (GUI.Button(new Rect(itemX, itemY, itemW, 38f), rstText, rstStyle))
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
        itemY += 48f;
        GUIStyle clStyle = new GUIStyle(GUI.skin.button) { fontSize = 16, fontStyle = FontStyle.Bold };
        clStyle.normal.textColor = Color.white;
        if (GUI.Button(new Rect(itemX, itemY, itemW, 40f), "CLOSE SETTINGS", clStyle))
        {
            confirmResetHighScore = false;
            isSettingsOpen = false;
            AudioManager.Instance?.PlayClick();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 6. STATS SCREEN
    // ─────────────────────────────────────────────────────────────────────────
    private void DrawStatsScreen()
    {
        float sw = Screen.width;
        float sh = Screen.height;

        DrawOverlay(new Rect(0, 0, sw, sh), new Color(0f, 0f, 0f, 0.78f));

        float modalW = Mathf.Clamp(sw * 0.82f, 320f, 440f);
        float modalH = 370f;
        float modalX = (sw - modalW) / 2f;
        float modalY = (sh - modalH) / 2f;

        DrawCard(new Rect(modalX, modalY, modalW, modalH),
            new Color(0.11f, 0.13f, 0.18f, 0.97f), new Color(0.3f, 0.9f, 0.4f, 0.85f), 3f);

        GUIStyle sTitle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 28,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        DrawShadowLabel(new Rect(modalX, modalY + 18, modalW, 40), "RUNNER STATS", sTitle,
            new Color(0.3f, 0.9f, 0.4f), new Color(0f, 0f, 0f, 0.8f), 2f);

        Rect sBox = new Rect(modalX + 25f, modalY + 68f, modalW - 50f, 215f);
        DrawCard(sBox, new Color(0.06f, 0.08f, 0.12f, 0.85f), new Color(0.3f, 0.4f, 0.5f, 0.5f), 1f);

        int bestScore = Mathf.FloorToInt(PlayerPrefs.GetFloat("HighScore", 0f));
        int totalRuns = PlayerPrefs.GetInt("TotalRuns", 0);
        float totalDist = PlayerPrefs.GetFloat("TotalDistance", 0f);
        int totalCoins = ShopManager.Instance != null ? ShopManager.Instance.GetTotalCoins() : 0;
        int boardsStock = ShopManager.Instance != null ? ShopManager.Instance.GetHoverboardStock() : 0;

        GUIStyle lineStyle = new GUIStyle(GUI.skin.label) { fontSize = 15, fontStyle = FontStyle.Bold };

        lineStyle.normal.textColor = Color.yellow;
        GUI.Label(new Rect(sBox.x + 16, sBox.y + 14, sBox.width - 32, 24), "🏆 Best Distance: " + bestScore + " m", lineStyle);

        lineStyle.normal.textColor = Color.white;
        GUI.Label(new Rect(sBox.x + 16, sBox.y + 48, sBox.width - 32, 24), "🏃 Total Runs: " + totalRuns, lineStyle);

        lineStyle.normal.textColor = Color.cyan;
        GUI.Label(new Rect(sBox.x + 16, sBox.y + 82, sBox.width - 32, 24), "📏 Lifetime Distance: " + Mathf.FloorToInt(totalDist) + " m", lineStyle);

        lineStyle.normal.textColor = Color.yellow;
        GUI.Label(new Rect(sBox.x + 16, sBox.y + 116, sBox.width - 32, 24), "🪙 Total Bank Coins: " + totalCoins, lineStyle);

        lineStyle.normal.textColor = new Color(0.3f, 0.9f, 0.5f);
        GUI.Label(new Rect(sBox.x + 16, sBox.y + 150, sBox.width - 32, 24), "🛹 Hoverboards in Stock: " + boardsStock, lineStyle);

        // Close Button
        GUIStyle clStyle = new GUIStyle(GUI.skin.button) { fontSize = 16, fontStyle = FontStyle.Bold };
        clStyle.normal.textColor = Color.white;
        if (GUI.Button(new Rect(modalX + 25f, modalY + 300f, modalW - 50f, 44f), "CLOSE STATS", clStyle))
        {
            isStatsOpen = false;
            AudioManager.Instance?.PlayClick();
        }
    }
}
