using UnityEngine;
using System;

/// <summary>
/// Manages Discord OAuth2 Sign-In flow
/// </summary>
public class DiscordAuthManager : MonoBehaviour
{
    public static DiscordAuthManager Instance { get; private set; }
    
    [Header("Discord App Settings")]
    [Tooltip("Client ID from Discord Developer Portal")]
    public string clientId = "1554490437304057946";
    
    [Tooltip("Must exactly match the redirect URI set in Discord Developer Portal")]
    public string redirectUri = "https://game.winjogames.com/discord-callback";
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Opens the device browser to Discord's OAuth authorization page.
    /// After authorizing, Discord redirects to the redirectUri, which must 
    /// deep-link back into the app as blingodice://open/discord-callback?code=...
    /// </summary>
    public void StartDiscordLogin()
    {
        if (string.IsNullOrEmpty(clientId))
        {
            Debug.LogError("[DiscordAuthManager] Client ID is missing. Set it in the Inspector.");
            return;
        }

        string discordAuthUrl = $"https://discord.com/api/oauth2/authorize?client_id={clientId}&redirect_uri={Uri.EscapeDataString(redirectUri)}&response_type=code&scope=identify%20email";
        Debug.Log($"[DiscordAuthManager] Opening URL: {discordAuthUrl}");
        
        Application.OpenURL(discordAuthUrl);
    }
}
