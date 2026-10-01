using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public bool isGameOver = false;
    public float score = 0f;
    public int coins = 0;
    private Transform player;

    void Awake()
    {
        Instance = this;
        // Lock mobile device orientation to Portrait (Subway Surfers vertical standard)
        Screen.orientation = ScreenOrientation.Portrait;
    }

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }

    void Update()
    {
        if (!isGameOver && player != null)
        {
            score = player.position.z;
        }
    }

    public void AddCoin()
    {
        coins++;
    }

    public void GameOver()
    {
        if (isGameOver) return;
        isGameOver = true;
        float highScore = PlayerPrefs.GetFloat("HighScore", 0f);
        if (score > highScore)
        {
            highScore = score;
            PlayerPrefs.SetFloat("HighScore", highScore);
        }
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

        void OnGUI()
    {
        int baseFontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height * 0.030f), 18, 28);

        // Score (Top Left)
        GUIStyle scoreStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = baseFontSize,
            fontStyle = FontStyle.Bold
        };
        scoreStyle.normal.textColor = Color.white;
        GUI.Label(new Rect(25, 20, 260, 40), "Score: " + Mathf.FloorToInt(score), scoreStyle);

        // Coins (Top Right)
        GUIStyle coinStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = baseFontSize,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleRight
        };
        coinStyle.normal.textColor = Color.yellow;
GUI.Label(new Rect(Screen.width - 260, 20, 180, 40), "COINS: " + coins, coinStyle);

        // Mute / Unmute Button
        string muteText = (AudioManager.Instance != null && AudioManager.Instance.isMuted) ? "MUTE" : "AUDIO";
        if (GUI.Button(new Rect(Screen.width - 70, 20, 52, 34), muteText))
        {
            AudioManager.Instance?.ToggleMute();
        }

        // Game Over Screen (Centered Responsive Modal)
        if (isGameOver)
        {
            float modalW = Mathf.Clamp(Screen.width * 0.75f, 320f, 440f);
            float modalH = 320f;
            float modalX = (Screen.width - modalW) / 2f;
            float modalY = (Screen.height - modalH) / 2f;

            GUI.Box(new Rect(modalX, modalY, modalW, modalH), "");

            GUIStyle goStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 38,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            goStyle.normal.textColor = new Color(1f, 0.25f, 0.25f);
            GUI.Label(new Rect(modalX, modalY + 20, modalW, 50), "GAME OVER", goStyle);

            GUIStyle fsStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            fsStyle.normal.textColor = Color.white;
            GUI.Label(new Rect(modalX, modalY + 85, modalW, 30), "Score: " + Mathf.FloorToInt(score), fsStyle);

            GUIStyle hsStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            hsStyle.normal.textColor = Color.yellow;
            GUI.Label(new Rect(modalX, modalY + 125, modalW, 30), "High Score: " + Mathf.FloorToInt(PlayerPrefs.GetFloat("HighScore", 0f)), hsStyle);

            GUIStyle coinSummaryStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                alignment = TextAnchor.MiddleCenter
            };
            coinSummaryStyle.normal.textColor = Color.yellow;
            GUI.Label(new Rect(modalX, modalY + 165, modalW, 30), "Coins: " + coins, coinSummaryStyle);

            GUIStyle btnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold
            };
            if (GUI.Button(new Rect(modalX + 45, modalY + 225, modalW - 90, 60), "▶ PLAY AGAIN", btnStyle))
            {
                RestartGame();
            }
        }
    }
}