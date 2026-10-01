using UnityEngine;

public class ShopManager : MonoBehaviour
{
    private static ShopManager _instance;
    public static ShopManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Object.FindFirstObjectByType<ShopManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("ShopManager");
                    _instance = go.AddComponent<ShopManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoInitialize()
    {
        if (_instance == null)
        {
            var dummy = Instance;
        }
    }

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
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
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

        float sw = Screen.width;
        float sh = Screen.height;
        float scale = Mathf.Clamp(sw / 400f, 0.80f, 1.35f);

        // Dark navy vignette backdrop
        SubwayUI.DrawRect(new Rect(0, 0, sw, sh), new Color(0.04f, 0.06f, 0.10f, 0.90f));

        // Modal Dimensions
        float modalW = Mathf.Clamp(sw * 0.90f, 320f, 460f);
        float modalH = Mathf.Clamp(sh * 0.82f, 450f, 580f);
        float modalX = (sw - modalW) / 2f;
        float modalY = (sh - modalH) / 2f;

        // Card Container
        SubwayUI.DrawCard(new Rect(modalX, modalY, modalW, modalH), SubwayUI.DarkCard, SubwayUI.GoldBorder, 3f);

        // 1. Header & Total Currency
        SubwayUI.DrawExtrudedText(new Rect(modalX, modalY + (12f * scale), modalW, 36f * scale),
            "SURF SHOP", Mathf.RoundToInt(28 * scale), Color.yellow, new Color(0.35f, 0.15f, 0f, 0.85f), 3);

        Rect coinRect = new Rect(modalX + 20f, modalY + (50f * scale), modalW - 40f, 36f * scale);
        SubwayUI.DrawPillBadge(coinRect, "🪙 TOTAL COINS", GetTotalCoins().ToString("N0"), SubwayUI.GoldBorder, Color.yellow, Mathf.RoundToInt(15 * scale));

        // 2. Navigation Tabs
        float tabW = (modalW - 40f) / 2f;
        float tabH = 38f * scale;
        float tabY = modalY + (94f * scale);

        Rect tab0Rect = new Rect(modalX + 20f, tabY, tabW - 4f, tabH);
        Color tab0Face = (activeTab == 0) ? SubwayUI.BlueFace : SubwayUI.InnerCard;
        Color tab0Bevel = (activeTab == 0) ? SubwayUI.BlueBevel : SubwayUI.GrayBorder;
        Color tab0Text = (activeTab == 0) ? Color.white : Color.gray;
        if (SubwayUI.DrawChunkyButton(tab0Rect, "⚡ GEAR & UPGRADES", tab0Face, tab0Bevel, tab0Text, Mathf.RoundToInt(12 * scale)))
        {
            activeTab = 0;
            AudioManager.Instance?.PlayClick();
        }

        Rect tab1Rect = new Rect(modalX + 20f + tabW + 4f, tabY, tabW - 4f, tabH);
        Color tab1Face = (activeTab == 1) ? SubwayUI.PurpleFace : SubwayUI.InnerCard;
        Color tab1Bevel = (activeTab == 1) ? SubwayUI.PurpleBevel : SubwayUI.GrayBorder;
        Color tab1Text = (activeTab == 1) ? Color.white : Color.gray;
        if (SubwayUI.DrawChunkyButton(tab1Rect, "🛹 BOARD SKINS", tab1Face, tab1Bevel, tab1Text, Mathf.RoundToInt(12 * scale)))
        {
            activeTab = 1;
            AudioManager.Instance?.PlayClick();
        }

        float contentY = tabY + tabH + (12f * scale);
        float itemW = modalW - 40f;
        float itemH = 60f * scale;
        float itemSpacing = 8f * scale;

        // 3. Tab Content
        if (activeTab == 0)
        {
            // Item A: Single Hoverboard
            DrawShopRow(modalX + 20f, contentY, itemW, itemH,
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
                }, false, scale);

            // Item B: Hoverboard 3-Pack
            DrawShopRow(modalX + 20f, contentY + itemH + itemSpacing, itemW, itemH,
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
                }, false, scale);

            // Item C: Duration Upgrade
            int curLevel = GetHoverboardLevel();
            string upDesc = curLevel < 4 
                ? string.Format("Duration: {0}s -> {1}s", UPGRADE_DURATIONS[curLevel - 1], UPGRADE_DURATIONS[curLevel])
                : string.Format("Max Level: {0}s Shield", UPGRADE_DURATIONS[curLevel - 1]);
            string btnText = curLevel < 4 ? string.Format("UPGRADE {0}", UPGRADE_COSTS[curLevel]) : "MAXED";

            DrawShopRow(modalX + 20f, contentY + (itemH + itemSpacing) * 2f, itemW, itemH,
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
                }, curLevel >= 4, scale);
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

                DrawShopRow(modalX + 20f, contentY + (i * (itemH + itemSpacing)), itemW, itemH,
                    skin.displayName,
                    isEquipped ? "Currently Surfing" : (isUnlocked ? "Unlocked & Ready" : "Exclusive Board Style"),
                    actionText,
                    () =>
                    {
                        if (isEquipped) return;
                        if (isUnlocked)
                        {
                            EquipSkin(skin.id);
                            AudioManager.Instance?.PlayClick();
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
                    }, isEquipped, scale);
            }
        }

        // 4. Feedback Message Banner
        if (!string.IsNullOrEmpty(feedbackMsg))
        {
            GUIStyle fbStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(14 * scale),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            fbStyle.normal.textColor = feedbackMsg.Contains("Not enough") ? Color.red : Color.green;
            GUI.Label(new Rect(modalX, modalY + modalH - (88f * scale), modalW, 26f), feedbackMsg, fbStyle);
        }

        // 5. Close Button (Chunky Red Candy Button)
        Rect closeRect = new Rect(modalX + 20f, modalY + modalH - (54f * scale), modalW - 40f, 44f * scale);
        if (SubwayUI.DrawChunkyButton(closeRect, "✕ CLOSE SHOP", SubwayUI.RedFace, SubwayUI.RedBevel, Color.white, Mathf.RoundToInt(15 * scale)))
        {
            CloseShop();
            AudioManager.Instance?.PlayClick();
        }
    }

    private void DrawShopRow(float x, float y, float w, float h, string title, string sub, string action, System.Action onAction, bool disabled = false, float scale = 1.0f)
    {
        SubwayUI.DrawCard(new Rect(x, y, w, h), SubwayUI.InnerCard, SubwayUI.GrayBorder, 1.5f);

        GUIStyle tStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(14 * scale),
            fontStyle = FontStyle.Bold
        };
        tStyle.normal.textColor = Color.white;
        GUI.Label(new Rect(x + 12f, y + (8f * scale), w - (125f * scale), 22f), title, tStyle);

        GUIStyle sStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(11 * scale)
        };
        sStyle.normal.textColor = Color.gray;
        GUI.Label(new Rect(x + 12f, y + (30f * scale), w - (125f * scale), 22f), sub, sStyle);

        // Action Button
        float btnW = 105f * scale;
        float btnH = 40f * scale;
        Rect btnRect = new Rect(x + w - btnW - 10f, y + ((h - btnH) / 2f), btnW, btnH);

        Color face = SubwayUI.YellowFace;
        Color bevel = SubwayUI.YellowBevel;
        Color txtCol = Color.black;

        if (action == "EQUIPPED")
        {
            face = SubwayUI.DarkCard;
            bevel = SubwayUI.InnerCard;
            txtCol = Color.cyan;
        }
        else if (action == "EQUIP")
        {
            face = SubwayUI.GreenFace;
            bevel = SubwayUI.GreenBevel;
            txtCol = Color.white;
        }

        if (SubwayUI.DrawChunkyButton(btnRect, action, face, bevel, txtCol, Mathf.RoundToInt(13 * scale)))
        {
            if (!disabled && onAction != null)
            {
                onAction();
            }
        }
    }
}
