using UnityEngine;

public class AudioManager : MonoBehaviour
{
    private static AudioManager _instance;
    public static AudioManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Object.FindFirstObjectByType<AudioManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("AudioManager");
                    _instance = go.AddComponent<AudioManager>();
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

    [Header("Audio Sources")]
    public AudioSource bgmSource;
    public AudioSource sfxSource;
    public AudioSource coinSource;

    [Header("Audio Clips")]
    public AudioClip bgmClip;
    public AudioClip coinClip;
    public AudioClip jumpClip;
    public AudioClip slideClip;
    public AudioClip hoverboardClip;
    public AudioClip shieldSaveClip;
    public AudioClip crashClip;
    public AudioClip powerupClip;

    [Header("Volume Controls")]
    [Range(0f, 1f)] public float bgmVolume = 0.55f;
    [Range(0f, 1f)] public float sfxVolume = 0.80f;
    public bool isMuted = false;

    // Dynamic pitch escalation for rapid coin streaks (Subway Surfers signature!)
    private int coinStreak = 0;
    private float lastCoinTime = 0f;

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

        // Initialize AudioSources if not assigned
        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
        }
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
        }
        if (coinSource == null)
        {
            coinSource = gameObject.AddComponent<AudioSource>();
            coinSource.loop = false;
            coinSource.playOnAwake = false;
        }

        EnsureClipsLoaded();

        // Read saved mute preference
        isMuted = PlayerPrefs.GetInt("AudioMuted", 0) == 1;
        UpdateAudioVolumes();
    }

    public void EnsureClipsLoaded()
    {
        if (bgmClip == null) bgmClip = LoadClip("bgm");
        if (coinClip == null) coinClip = LoadClip("coin");
        if (jumpClip == null) jumpClip = LoadClip("jump");
        if (slideClip == null) slideClip = LoadClip("slide");
        if (hoverboardClip == null) hoverboardClip = LoadClip("hoverboard");
        if (shieldSaveClip == null) shieldSaveClip = LoadClip("shield_save");
        if (crashClip == null) crashClip = LoadClip("crash");
        if (powerupClip == null) powerupClip = LoadClip("powerup");
    }

    private AudioClip LoadClip(string clipName)
    {
        AudioClip clip = Resources.Load<AudioClip>("Audio/" + clipName);
#if UNITY_EDITOR
        if (clip == null)
        {
            clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/" + clipName + ".wav");
        }
#endif
        return clip;
    }

    void Start()
    {
        PlayBGM();
    }

    public void UpdateAudioVolumes()
    {
        if (bgmSource != null) bgmSource.volume = isMuted ? 0f : bgmVolume;
        if (sfxSource != null) sfxSource.volume = isMuted ? 0f : sfxVolume;
        if (coinSource != null) coinSource.volume = isMuted ? 0f : sfxVolume;
    }

    public void ToggleMute()
    {
        isMuted = !isMuted;
        PlayerPrefs.SetInt("AudioMuted", isMuted ? 1 : 0);
        PlayerPrefs.Save();
        UpdateAudioVolumes();
    }

    // ── BGM Control ──────────────────────────────────────────────────────────
    public void PlayBGM()
    {
        if (bgmClip == null) EnsureClipsLoaded();
        if (bgmSource != null && bgmClip != null)
        {
            if (bgmSource.clip != bgmClip)
            {
                bgmSource.clip = bgmClip;
            }
            bgmSource.volume = isMuted ? 0f : bgmVolume;
            if (!bgmSource.isPlaying)
            {
                bgmSource.Play();
            }
        }
    }

    public void PauseBGM()
    {
        if (bgmSource != null && bgmSource.isPlaying)
        {
            bgmSource.Pause();
        }
    }

    public void StopBGM()
    {
        if (bgmSource != null)
        {
            bgmSource.Stop();
        }
    }

    // ── SFX Triggers ─────────────────────────────────────────────────────────

    /// <summary>
    /// Plays the coin collection sound with an escalating musical pitch ladder
    /// whenever coins are collected in rapid succession.
    /// </summary>
    public void PlayCoinSound()
    {
        if (isMuted) return;
        if (coinClip == null) EnsureClipsLoaded();
        if (coinSource == null || coinClip == null) return;

        // If collected within 0.70 seconds of previous coin, climb musical semitones!
        if (Time.time - lastCoinTime < 0.70f)
        {
            coinStreak = Mathf.Min(coinStreak + 1, 12);
        }
        else
        {
            coinStreak = 0;
        }
        lastCoinTime = Time.time;

        // ~1 semitone per coin in streak (2^(streak/12))
        float pitchMultiplier = Mathf.Pow(1.05946f, coinStreak);
        coinSource.pitch = pitchMultiplier;
        coinSource.PlayOneShot(coinClip, sfxVolume);
    }

    public void PlayJump()
    {
        if (jumpClip == null) EnsureClipsLoaded();
        PlaySFX(jumpClip, 0.75f);
    }

    public void PlaySlide()
    {
        if (slideClip == null) EnsureClipsLoaded();
        PlaySFX(slideClip, 0.70f);
    }

    public void PlayHoverboardDeploy()
    {
        if (hoverboardClip == null) EnsureClipsLoaded();
        PlaySFX(hoverboardClip, 0.85f);
    }

    public void PlayShieldSave()
    {
        if (shieldSaveClip == null) EnsureClipsLoaded();
        PlaySFX(shieldSaveClip, 0.95f);
    }

    public void PlayCrash()
    {
        if (crashClip == null) EnsureClipsLoaded();
        PlaySFX(crashClip, 1.0f);
        PauseBGM();
    }

    public void PlayPowerup()
    {
        if (powerupClip == null) EnsureClipsLoaded();
        PlaySFX(powerupClip, 0.90f);
    }

    private void PlaySFX(AudioClip clip, float volumeScale = 1.0f)
    {
        if (isMuted || sfxSource == null || clip == null) return;
        sfxSource.pitch = Random.Range(0.97f, 1.03f); // Subtle natural variation
        sfxSource.PlayOneShot(clip, sfxVolume * volumeScale);
    }
}
