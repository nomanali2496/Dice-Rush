using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Centralized audio manager
/// Handles ALL audio playback: music (background + gameplay) and sound effects
/// Auto-creates all AudioSources at runtime - just assign clips!
/// Checks SettingManager for music/sound enabled/disabled state
/// </summary>
public class SoundManager : MonoBehaviour
{
    public static SoundManager instance;
    
    [Header("Music Clips")]
    [SerializeField] private AudioClip backgroundMusicClip; // Main Menu / Lobby background music
    [SerializeField] private AudioClip gameplayMusicClip; // Gameplay background music
    
    // Music AudioSources (auto-created at runtime)
    private AudioSource backgroundMusicSource;
    private AudioSource gameplayMusicSource;
    
    [Header("SFX Pool Settings")]
    [SerializeField] private int sfxPoolSize = 6; // Pool size for overlapping sounds (cash, chips, etc.)
    private List<AudioSource> sfxPool = new List<AudioSource>();
    
    [Header("Sound Effect Clips")]
    [SerializeField] private AudioClip tapClip; // UI tap/click sound
    [SerializeField] private AudioClip countdownClip; // Countdown sound (3, 2, 1)
    [SerializeField] private AudioClip cashClip; // Cash reward sound
    [SerializeField] private AudioClip chipsClip; // Chips reward sound
    [SerializeField] private AudioClip correctClip; // Correct answer/selection sound
    [SerializeField] private AudioClip wrongClip; // Wrong answer/selection sound
    
    private void Awake()
    {
        // Singleton pattern
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        InitializeMusicSources();
        InitializeSFXPool();
    }
    
    /// <summary>
    /// Auto-creates music AudioSources and assigns clips
    /// </summary>
    private void InitializeMusicSources()
    {
        // Create Background Music Source
        backgroundMusicSource = gameObject.AddComponent<AudioSource>();
        backgroundMusicSource.loop = true;
        backgroundMusicSource.playOnAwake = false;
        backgroundMusicSource.clip = backgroundMusicClip;
        
        if (backgroundMusicClip != null)
        {
            Debug.Log("[SoundManager] Background music source created and clip assigned");
        }
        else
        {
            Debug.LogWarning("[SoundManager] Background music clip not assigned!");
        }
        
        // Create Gameplay Music Source
        gameplayMusicSource = gameObject.AddComponent<AudioSource>();
        gameplayMusicSource.loop = true;
        gameplayMusicSource.playOnAwake = false;
        gameplayMusicSource.clip = gameplayMusicClip;
        
        if (gameplayMusicClip != null)
        {
            Debug.Log("[SoundManager] Gameplay music source created and clip assigned");
        }
        else
        {
            Debug.LogWarning("[SoundManager] Gameplay music clip not assigned!");
        }
    }
    
    /// <summary>
    /// Creates a pool of AudioSources for simultaneous sound playback
    /// </summary>
    private void InitializeSFXPool()
    {
        for (int i = 0; i < sfxPoolSize; i++)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            sfxPool.Add(source);
        }
        
        Debug.Log($"[SoundManager] SFX pool initialized with {sfxPoolSize} AudioSources");
    }
    
    #region Music Playback
    
    /// <summary>
    /// Plays background music (lobby/main menu)
    /// Checks SettingManager for music enabled state
    /// </summary>
    public void PlayBackgroundMusic()
    {
        if (SettingManager.instance != null && !SettingManager.instance.IsMusicEnabled())
        {
            return; // Music disabled
        }
        
        if (backgroundMusicSource != null && !backgroundMusicSource.isPlaying)
        {
            backgroundMusicSource.Play();
            Debug.Log("[SoundManager] Playing background music");
        }
    }
    
    /// <summary>
    /// Stops background music
    /// </summary>
    public void StopBackgroundMusic()
    {
        if (backgroundMusicSource != null && backgroundMusicSource.isPlaying)
        {
            backgroundMusicSource.Stop();
            Debug.Log("[SoundManager] Stopped background music");
        }
    }
    
    /// <summary>
    /// Plays gameplay music
    /// Checks SettingManager for music enabled state
    /// </summary>
    public void PlayGameplayMusic()
    {
        if (SettingManager.instance != null && !SettingManager.instance.IsMusicEnabled())
        {
            return; // Music disabled
        }
        
        if (gameplayMusicSource != null && !gameplayMusicSource.isPlaying)
        {
            gameplayMusicSource.Play();
            Debug.Log("[SoundManager] Playing gameplay music");
        }
    }
    
    /// <summary>
    /// Stops gameplay music
    /// </summary>
    public void StopGameplayMusic()
    {
        if (gameplayMusicSource != null && gameplayMusicSource.isPlaying)
        {
            gameplayMusicSource.Stop();
            Debug.Log("[SoundManager] Stopped gameplay music");
        }
    }
    
    /// <summary>
    /// Stops all music (background + gameplay)
    /// </summary>
    public void StopAllMusic()
    {
        StopBackgroundMusic();
        StopGameplayMusic();
    }
    
    #endregion
    
    #region Sound Effects Playback
    
    /// <summary>
    /// Plays a sound effect using the first available AudioSource from the pool
    /// Checks SettingManager to see if sound is enabled
    /// </summary>
    public void PlaySFX(AudioClip clip)
    {
        // Check if sound is enabled in SettingManager
        if (SettingManager.instance != null && !SettingManager.instance.IsSoundEnabled())
        {
            return; // Sound is disabled, don't play
        }
        
        if (clip == null)
        {
            Debug.LogWarning("[SoundManager] Cannot play SFX - AudioClip is null!");
            return;
        }
        
        // Find an available AudioSource from the pool
        AudioSource availableSource = GetAvailableSource();
        if (availableSource != null)
        {
            availableSource.PlayOneShot(clip);
        }
        else
        {
            Debug.LogWarning("[SoundManager] No available AudioSource in pool!");
        }
    }
    
    /// <summary>
    /// Plays tap/click sound effect
    /// </summary>
    public void PlayTap()
    {
        if (tapClip != null)
        {
            PlaySFX(tapClip);
        }
        else
        {
            Debug.LogWarning("[SoundManager] Tap clip not assigned!");
        }
    }
    
    /// <summary>
    /// Plays countdown sound effect (for 3, 2, 1 countdown)
    /// </summary>
    public void PlayCountdown()
    {
        if (countdownClip != null)
        {
            PlaySFX(countdownClip);
        }
        else
        {
            Debug.LogWarning("[SoundManager] Countdown clip not assigned!");
        }
    }
    
    /// <summary>
    /// Plays cash reward sound effect
    /// </summary>
    public void PlayCash()
    {
        if (cashClip != null)
        {
            PlaySFX(cashClip);
        }
        else
        {
            Debug.LogWarning("[SoundManager] Cash clip not assigned!");
        }
    }
    
    /// <summary>
    /// Plays chips reward sound effect
    /// </summary>
    public void PlayChips()
    {
        if (chipsClip != null)
        {
            PlaySFX(chipsClip);
        }
        else
        {
            Debug.LogWarning("[SoundManager] Chips clip not assigned!");
        }
    }
    
    /// <summary>
    /// Plays correct answer/selection sound effect
    /// </summary>
    public void PlayCorrect()
    {
        if (correctClip != null)
        {
            PlaySFX(correctClip);
        }
        else
        {
            Debug.LogWarning("[SoundManager] Correct clip not assigned!");
        }
    }
    
    /// <summary>
    /// Plays wrong answer/selection sound effect
    /// </summary>
    public void PlayWrong()
    {
        if (wrongClip != null)
        {
            PlaySFX(wrongClip);
        }
        else
        {
            Debug.LogWarning("[SoundManager] Wrong clip not assigned!");
        }
    }
    
    /// <summary>
    /// Finds an available (not currently playing) AudioSource from the pool
    /// If all are busy, returns the first one (will overlap)
    /// </summary>
    private AudioSource GetAvailableSource()
    {
        // First, try to find a source that's not playing
        foreach (AudioSource source in sfxPool)
        {
            if (!source.isPlaying)
            {
                return source;
            }
        }
        
        // If all sources are busy, use the first one anyway (will overlap)
        if (sfxPool.Count > 0)
        {
            return sfxPool[0];
        }
        
        return null;
    }
    
    /// <summary>
    /// Stops all currently playing sound effects
    /// </summary>
    public void StopAllSFX()
    {
        foreach (AudioSource source in sfxPool)
        {
            if (source.isPlaying)
            {
                source.Stop();
            }
        }
        Debug.Log("[SoundManager] All SFX stopped");
    }
    
    #endregion
}
