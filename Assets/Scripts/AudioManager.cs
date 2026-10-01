using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

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
    [Range(0f, 1f)] public float bgmVolume = 0.60f;
    [Range(0f, 1f)] public float sfxVolume = 0.85f;
    public bool isMuted = false;

    // Dynamic pitch escalation for rapid coin streaks (Subway Surfers signature!)
    private int coinStreak = 0;
    private float lastCoinTime = 0f;

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

        // Read saved mute preference
        isMuted = PlayerPrefs.GetInt("AudioMuted", 0) == 1;
        UpdateAudioVolumes();
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
        if (isMuted || coinSource == null || coinClip == null) return;

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
        PlaySFX(jumpClip, 0.85f);
    }

    public void PlaySlide()
    {
        PlaySFX(slideClip, 0.75f);
    }

    public void PlayHoverboardDeploy()
    {
        PlaySFX(hoverboardClip, 0.90f);
    }

    public void PlayShieldSave()
    {
        PlaySFX(shieldSaveClip, 1.0f);
    }

    public void PlayCrash()
    {
        PlaySFX(crashClip, 1.0f);
        PauseBGM();
    }

    public void PlayPowerup()
    {
        PlaySFX(powerupClip, 0.95f);
    }

    private void PlaySFX(AudioClip clip, float volumeScale = 1.0f)
    {
        if (isMuted || sfxSource == null || clip == null) return;
        sfxSource.pitch = Random.Range(0.97f, 1.03f); // Subtle natural variation
        sfxSource.PlayOneShot(clip, sfxVolume * volumeScale);
    }
}
