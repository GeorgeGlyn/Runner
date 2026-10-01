using UnityEngine;

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance;

    public bool isShopOpen = false;

    // Persistent Storage Keys
    private const string KEY_TOTAL_COINS = "TotalCoins";
    private const string KEY_HOVERBOARD_STOCK = "HoverboardStock";
    private const string KEY_HOVERBOARD_LEVEL = "HoverboardLevel";
    private const string KEY_EQUIPPED_SKIN = "EquippedSkin";
    private const string KEY_SKIN_UNLOCKED_PREFIX = "SkinUnlocked_";

    // Pricing & Specs
    public const int PRICE_SINGLE_BOARD = 300;
    public const int PRICE_BOARD_PACK = 800; // 3 boards (save 100)
    public static readonly int[] UPGRADE_COSTS = new int[] { 0, 500, 1000, 2000 }; // Costs for Level 2, 3, 4
    public static readonly float[] UPGRADE_DURATIONS = new float[] { 15f, 18.5f, 22f, 26f };

    [System.Serializable]
    public struct BoardSkin
    {
        public string id;
        public string displayName;
        public int price;
        public Color primaryColor;

        public BoardSkin(string id, string name, int price, Color col)
        {
            this.id = id;
            this.displayName = name;
            this.price = price;
            this.primaryColor = col;
        }
    }

    public static readonly BoardSkin[] SKINS = new BoardSkin[]
    {
        new BoardSkin("Classic", "Classic Cyan", 0, new Color(0.1f, 0.9f, 0.95f)),
        new BoardSkin("Solar", "Solar Flame", 1200, new Color(1.0f, 0.38f, 0.05f)),
        new BoardSkin("Cyber", "Cyber Violet", 2200, new Color(0.85f, 0.15f, 0.95f))
    };

    private int activeTab = 0; // 0: Supplies & Upgrades, 1: Board Skins
    private string feedbackMsg = "";
    private float feedbackTimer = 0f;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // Initialize persistent starter balance on first run
        if (!PlayerPrefs.HasKey(KEY_HOVERBOARD_STOCK))
        {
            PlayerPrefs.SetInt(KEY_HOVERBOARD_STOCK, 3); // 3 starter boards
        }
        if (!PlayerPrefs.HasKey(KEY_TOTAL_COINS))
        {
            PlayerPrefs.SetInt(KEY_TOTAL_COINS, 300); // 300 starter welcome coins!
        }
        if (!PlayerPrefs.HasKey(KEY_HOVERBOARD_LEVEL))
        {
            PlayerPrefs.SetInt(KEY_HOVERBOARD_LEVEL, 1);
        }
        if (!PlayerPrefs.HasKey(KEY_EQUIPPED_SKIN))
        {
            PlayerPrefs.SetString(KEY_EQUIPPED_SKIN, "Classic");
        }
        PlayerPrefs.SetInt(KEY_SKIN_UNLOCKED_PREFIX + "Classic", 1);
        PlayerPrefs.Save();
    }

    void Update()
    {
        if (feedbackTimer > 0f)
        {
            feedbackTimer -= Time.unscaledDeltaTime;
            if (feedbackTimer <= 0f) feedbackMsg = "";
        }

        // Shortcut to toggle shop in PC / Editor: Tab or P
        if (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.P))
        {
            if (isShopOpen) CloseShop();
            else OpenShop();
        }
    }

    // ── Currency Management ───────────────────────────────────────────────────
    public int GetTotalCoins()
    {
        return PlayerPrefs.GetInt(KEY_TOTAL_COINS, 0);
    }

    public void AddCoins(int amount)
    {
        int current = GetTotalCoins();
        PlayerPrefs.SetInt(KEY_TOTAL_COINS, current + amount);
        PlayerPrefs.Save();
    }

    public bool SpendCoins(int amount)
    {
        int current = GetTotalCoins();
        if (current >= amount)
        {
            PlayerPrefs.SetInt(KEY_TOTAL_COINS, current - amount);
            PlayerPrefs.Save();
            return true;
        }
        return false;
    }

    // ── Hoverboard Stock & Upgrades ──────────────────────────────────────────
    public int GetHoverboardStock()
    {
        return PlayerPrefs.GetInt(KEY_HOVERBOARD_STOCK, 0);
    }

    public void AddHoverboard(int count = 1)
    {
        int cur = GetHoverboardStock();
        PlayerPrefs.SetInt(KEY_HOVERBOARD_STOCK, cur + count);
        PlayerPrefs.Save();
    }

    public bool ConsumeHoverboard()
    {
        int cur = GetHoverboardStock();
        if (cur > 0)
        {
            PlayerPrefs.SetInt(KEY_HOVERBOARD_STOCK, cur - 1);
            PlayerPrefs.Save();
            return true;
        }
        return false;
    }

    public int GetHoverboardLevel()
    {
        return Mathf.Clamp(PlayerPrefs.GetInt(KEY_HOVERBOARD_LEVEL, 1), 1, 4);
    }

    public float GetHoverboardDuration()
    {
        int level = GetHoverboardLevel();
        return UPGRADE_DURATIONS[level - 1];
    }

    // ── Skins Management ─────────────────────────────────────────────────────
    public string GetEquippedSkinId()
    {
        return PlayerPrefs.GetString(KEY_EQUIPPED_SKIN, "Classic");
    }

    public BoardSkin GetEquippedSkin()
    {
        string id = GetEquippedSkinId();
        foreach (var skin in SKINS)
        {
            if (skin.id == id) return skin;
        }
        return SKINS[0];
    }

    public bool IsSkinUnlocked(string skinId)
    {
        if (skinId == "Classic") return true;
        return PlayerPrefs.GetInt(KEY_SKIN_UNLOCKED_PREFIX + skinId, 0) == 1;
    }

    public void UnlockSkin(string skinId)
    {
        PlayerPrefs.SetInt(KEY_SKIN_UNLOCKED_PREFIX + skinId, 1);
        PlayerPrefs.Save();
    }

    public void EquipSkin(string skinId)
    {
        if (IsSkinUnlocked(skinId))
        {
            PlayerPrefs.SetString(KEY_EQUIPPED_SKIN, skinId);
            PlayerPrefs.Save();
            SetFeedback("Equipped: " + skinId);
            AudioManager.Instance?.PlayPowerup();
        }
    }

    // ── Shop Window Controls ─────────────────────────────────────────────────
    public void OpenShop()
    {
        isShopOpen = true;
        feedbackMsg = "";
    }

    public void CloseShop()
    {
        isShopOpen = false;
        feedbackMsg = "";
    }

    private void SetFeedback(string msg)
    {
        feedbackMsg = msg;
        feedbackTimer = 2.0f;
    }

    // ── Interactive Responsive Shop GUI ──────────────────────────────────────
    void OnGUI()
    {
        if (!isShopOpen) return;

        // Modal Dimensions
        float modalW = Mathf.Clamp(Screen.width * 0.88f, 330f, 480f);
        float modalH = Mathf.Clamp(Screen.height * 0.78f, 440f, 560f);
        float modalX = (Screen.width - modalW) / 2f;
        float modalY = (Screen.height - modalH) / 2f;

        // Background Box with Dark Subway Aesthetic
        GUI.Box(new Rect(modalX, modalY, modalW, modalH), "");

        // 1. Header & Total Currency
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height * 0.032f), 20, 28),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        titleStyle.normal.textColor = Color.white;
        GUI.Label(new Rect(modalX, modalY + 12f, modalW, 36f), "SURF SHOP", titleStyle);

        GUIStyle coinStyle = new GUIStyle(GUI.skin.box)
        {
            fontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height * 0.022f), 14, 18),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        coinStyle.normal.textColor = Color.yellow;
        GUI.Box(new Rect(modalX + 25f, modalY + 52f, modalW - 50f, 32f), "TOTAL COINS: " + GetTotalCoins(), coinStyle);

        // 2. Navigation Tabs
        float tabW = (modalW - 50f) / 2f;
        GUIStyle tabStyle0 = new GUIStyle(GUI.skin.button)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold
        };
        tabStyle0.normal.textColor = (activeTab == 0) ? Color.cyan : Color.gray;
        if (GUI.Button(new Rect(modalX + 25f, modalY + 92f, tabW - 4f, 34f), "GEAR & UPGRADES", tabStyle0))
        {
            activeTab = 0;
        }

        GUIStyle tabStyle1 = new GUIStyle(GUI.skin.button)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold
        };
        tabStyle1.normal.textColor = (activeTab == 1) ? Color.magenta : Color.gray;
        if (GUI.Button(new Rect(modalX + 25f + tabW + 4f, modalY + 92f, tabW - 4f, 34f), "BOARD SKINS", tabStyle1))
        {
            activeTab = 1;
        }

        float contentY = modalY + 136f;
        float itemW = modalW - 50f;

        // 3. Tab Content
        if (activeTab == 0)
        {
            // Item A: Single Hoverboard
            DrawShopRow(modalX + 25f, contentY, itemW, 58f,
                "Hoverboard (+1)",
                string.Format("Stock: {0} boards", GetHoverboardStock()),
                string.Format("BUY {0}", PRICE_SINGLE_BOARD),
                () =>
                {
                    if (SpendCoins(PRICE_SINGLE_BOARD))
                    {
                        AddHoverboard(1);
                        SetFeedback("+1 Hoverboard Purchased!");
                        AudioManager.Instance?.PlayPowerup();
                    }
                    else
                    {
                        SetFeedback("Not enough coins!");
                    }
                });

            // Item B: Hoverboard 3-Pack
            DrawShopRow(modalX + 25f, contentY + 66f, itemW, 58f,
                "Hoverboard 3-Pack",
                "Value Pack (Save 100 Coins!)",
                string.Format("BUY {0}", PRICE_BOARD_PACK),
                () =>
                {
                    if (SpendCoins(PRICE_BOARD_PACK))
                    {
                        AddHoverboard(3);
                        SetFeedback("+3 Hoverboards Added!");
                        AudioManager.Instance?.PlayPowerup();
                    }
                    else
                    {
                        SetFeedback("Not enough coins!");
                    }
                });

            // Item C: Duration Upgrade
            int curLevel = GetHoverboardLevel();
            string upDesc = curLevel < 4 
                ? string.Format("Duration: {0}s -> {1}s", UPGRADE_DURATIONS[curLevel - 1], UPGRADE_DURATIONS[curLevel])
                : string.Format("Max Level: {0}s Shield", UPGRADE_DURATIONS[curLevel - 1]);
            string btnText = curLevel < 4 ? string.Format("UPGRADE {0}", UPGRADE_COSTS[curLevel]) : "MAXED";

            DrawShopRow(modalX + 25f, contentY + 132f, itemW, 58f,
                string.Format("Shield Duration (Lv {0}/4)", curLevel),
                upDesc,
                btnText,
                () =>
                {
                    if (curLevel >= 4) return;
                    int cost = UPGRADE_COSTS[curLevel];
                    if (SpendCoins(cost))
                    {
                        PlayerPrefs.SetInt(KEY_HOVERBOARD_LEVEL, curLevel + 1);
                        PlayerPrefs.Save();
                        SetFeedback(string.Format("Upgraded to Level {0} ({1}s)!", curLevel + 1, UPGRADE_DURATIONS[curLevel]));
                        AudioManager.Instance?.PlayPowerup();
                    }
                    else
                    {
                        SetFeedback("Not enough coins!");
                    }
                }, curLevel >= 4);
        }
        else
        {
            // Tab 1: Board Skins
            string equippedId = GetEquippedSkinId();
            for (int i = 0; i < SKINS.Length; i++)
            {
                var skin = SKINS[i];
                bool isUnlocked = IsSkinUnlocked(skin.id);
                bool isEquipped = (equippedId == skin.id);

                string actionText;
                if (isEquipped) actionText = "EQUIPPED";
                else if (isUnlocked) actionText = "EQUIP";
                else actionText = string.Format("BUY {0}", skin.price);

                DrawShopRow(modalX + 25f, contentY + (i * 66f), itemW, 58f,
                    skin.displayName,
                    isEquipped ? "Currently Surfing" : (isUnlocked ? "Unlocked & Ready" : "Exclusive Board Style"),
                    actionText,
                    () =>
                    {
                        if (isEquipped) return;
                        if (isUnlocked)
                        {
                            EquipSkin(skin.id);
                        }
                        else
                        {
                            if (SpendCoins(skin.price))
                            {
                                UnlockSkin(skin.id);
                                EquipSkin(skin.id);
                                SetFeedback("Unlocked " + skin.displayName + "!");
                                AudioManager.Instance?.PlayPowerup();
                            }
                            else
                            {
                                SetFeedback("Not enough coins!");
                            }
                        }
                    }, isEquipped);
            }
        }

        // 4. Feedback Message Banner
        if (!string.IsNullOrEmpty(feedbackMsg))
        {
            GUIStyle fbStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            fbStyle.normal.textColor = feedbackMsg.Contains("Not enough") ? Color.red : Color.green;
            GUI.Label(new Rect(modalX, modalH + modalY - 80f, modalW, 26f), feedbackMsg, fbStyle);
        }

        // 5. Close Button
        GUIStyle closeStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold
        };
        closeStyle.normal.textColor = Color.white;
        if (GUI.Button(new Rect(modalX + 35f, modalH + modalY - 48f, modalW - 70f, 38f), "CLOSE SHOP", closeStyle))
        {
            CloseShop();
        }
    }

    private void DrawShopRow(float x, float y, float w, float h, string title, string sub, string action, System.Action onAction, bool disabled = false)
    {
        GUI.Box(new Rect(x, y, w, h), "");

        GUIStyle tStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold };
        tStyle.normal.textColor = Color.white;
        GUI.Label(new Rect(x + 10f, y + 6f, w - 120f, 22f), title, tStyle);

        GUIStyle sStyle = new GUIStyle(GUI.skin.label) { fontSize = 11 };
        sStyle.normal.textColor = Color.gray;
        GUI.Label(new Rect(x + 10f, y + 28f, w - 120f, 22f), sub, sStyle);

        GUIStyle btnStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold
        };
        btnStyle.normal.textColor = disabled ? Color.gray : Color.yellow;

        if (GUI.Button(new Rect(x + w - 105f, y + 10f, 96f, 38f), action, btnStyle))
        {
            if (!disabled && onAction != null)
            {
                onAction();
            }
        }
    }
}
