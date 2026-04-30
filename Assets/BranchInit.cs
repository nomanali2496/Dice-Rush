using UnityEngine;
using System.Collections.Generic;
using System;

public class BranchInit : MonoBehaviour
{
    public static BranchInit instance;
    private static bool isBranchInitialized = false;
    
    // Event to notify when Branch is initialized
    public static Action OnBranchReady;
    
    public static bool IsInitialized => isBranchInitialized;

    void Awake()
    {
        // Singleton pattern - ensure only one instance exists
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        
        Debug.Log("[BranchInit] BranchInit instance created");
    }

    void Start()
    {
        Debug.Log("[BranchInit] Starting Branch initialization...");
        
        // Enable debug logging if configured
        if (BranchData.Instance != null && BranchData.Instance.enableLogging)
        {
            Branch.enableLogging();
            Debug.Log("[BranchInit] Branch debug logging enabled");
        }

        // Enable tracking
        Branch.setTrackingDisabled(false);
        
        // Initialize Branch SDK
        Debug.Log("[BranchInit] Calling Branch.initSession()...");
        Branch.initSession(CallbackWithBranchUniversalObject);
    }

    /// <summary>
    /// Callback when Branch SDK finishes initializing
    /// </summary>
    void CallbackWithBranchUniversalObject(BranchUniversalObject buo,
        BranchLinkProperties linkProps,
        string error)
    {
        if (error != null)
        {
            Debug.Log("[BranchInit] ❌ Branch initialization FAILED: " + error);
            isBranchInitialized = false;
            return;
        }

        isBranchInitialized = true;
        Debug.Log("[BranchInit] ✅ Branch initialized SUCCESSFULLY!");
        Debug.Log("[BranchInit] Branch is ready to use");
        
        // Log deep link data if available
        if (linkProps != null && linkProps.controlParams != null && linkProps.controlParams.Count > 0)
        {
            Debug.Log("[BranchInit] 📱 Deep link detected! App opened from Branch link");
            Debug.Log("[BranchInit] Deep link params: " + linkProps.ToJsonString());
            
            if (buo != null)
            {
                Debug.Log("[BranchInit] Universal Object: " + buo.ToJsonString());
            }
            
            // Handle deep link data
            HandleDeepLinkData(buo, linkProps);
        }
        else
        {
            Debug.Log("[BranchInit] ℹ️ App opened normally (no deep link)");
        }
        
        // Notify that Branch is ready
        OnBranchReady?.Invoke();
    }

    /// <summary>
    /// Handle deep link data when app opens from a Branch link
    /// </summary>
    void HandleDeepLinkData(BranchUniversalObject buo, BranchLinkProperties linkProps)
    {
        if (linkProps == null || linkProps.controlParams == null) return;

        Debug.Log("[BranchInit] Processing deep link data...");
        
        // Check if this is a first-time open from a Branch link
        bool isFirstSession = false;
        bool clickedBranchLink = false;
        
        if (linkProps.controlParams.ContainsKey("+is_first_session"))
        {
            object firstSessionValue = linkProps.controlParams["+is_first_session"];
            if (firstSessionValue is bool)
            {
                isFirstSession = (bool)firstSessionValue;
            }
            else if (firstSessionValue is string)
            {
                bool.TryParse((string)firstSessionValue, out isFirstSession);
            }
        }
        
        if (linkProps.controlParams.ContainsKey("+clicked_branch_link"))
        {
            object clickedValue = linkProps.controlParams["+clicked_branch_link"];
            if (clickedValue is bool)
            {
                clickedBranchLink = (bool)clickedValue;
            }
            else if (clickedValue is string)
            {
                bool.TryParse((string)clickedValue, out clickedBranchLink);
            }
        }
        
        // If this is first session from Branch link, save referral data
        if (isFirstSession && clickedBranchLink)
        {
            Debug.Log("[BranchInit] 🎉 First-time open from Branch link detected!");
            
            // Extract referrer information
            string inviterPlayFabId = null;
            string inviterName = null;
            
            // Try to get from control params first
            if (linkProps.controlParams.ContainsKey("inviter_playfab_id"))
            {
                inviterPlayFabId = linkProps.controlParams["inviter_playfab_id"].ToString();
            }
            else if (buo != null && buo.metadata != null)
            {
                // Try to get from universal object metadata
                Dictionary<string, string> customMetadata = buo.metadata.GetCustomMetadata();
                if (customMetadata != null && customMetadata.ContainsKey("inviter_playfab_id"))
                {
                    inviterPlayFabId = customMetadata["inviter_playfab_id"];
                }
            }
            
            if (linkProps.controlParams.ContainsKey("inviter_name"))
            {
                inviterName = linkProps.controlParams["inviter_name"].ToString();
            }
            else if (buo != null && buo.metadata != null)
            {
                Dictionary<string, string> customMetadata = buo.metadata.GetCustomMetadata();
                if (customMetadata != null && customMetadata.ContainsKey("inviter_name"))
                {
                    inviterName = customMetadata["inviter_name"];
                }
            }
            
            if (!string.IsNullOrEmpty(inviterPlayFabId))
            {
                Debug.Log($"[BranchInit] Referrer found - ID: {inviterPlayFabId}, Name: {inviterName ?? "Unknown"}");
                
                // Store referral data temporarily until PlayFab login completes
                PlayerPrefs.SetString("PendingReferrerPlayFabId", inviterPlayFabId);
                if (!string.IsNullOrEmpty(inviterName))
                {
                    PlayerPrefs.SetString("PendingReferrerName", inviterName);
                }
                PlayerPrefs.SetInt("HasPendingReferral", 1);
                PlayerPrefs.Save();
                
                Debug.Log("[BranchInit] Referral data saved. Will be processed after PlayFab login.");
                
                // Try to process referral if already logged in
                if (PlayFabManager.Instance != null && PlayFabManager.Instance.IsLoggedIn())
                {
                    ProcessReferralAfterLogin(inviterPlayFabId, inviterName);
                }
            }
            else
            {
                Debug.LogWarning("[BranchInit] No referrer information found in Branch link");
            }
        }
        
        // Check if user came from an invite link
        if (linkProps.controlParams.ContainsKey("invite_type"))
        {
            string inviteType = linkProps.controlParams["invite_type"].ToString();
            Debug.Log($"[BranchInit] 🎁 Invite detected! Type: {inviteType}");
        }
        
        // Log all parameters for debugging
        foreach (var param in linkProps.controlParams)
        {
            Debug.Log($"[BranchInit] Deep link param: {param.Key} = {param.Value}");
        }
    }
    
    /// <summary>
    /// Process referral after PlayFab login completes
    /// This should be called from PlayFabManager after login
    /// </summary>
    public void ProcessReferralAfterLogin(string inviterPlayFabId, string inviterName = null)
    {
        if (PlayFabManager.Instance == null || !PlayFabManager.Instance.IsLoggedIn())
        {
            Debug.LogWarning("[BranchInit] Cannot process referral - not logged into PlayFab");
            return;
        }
        
        string currentUserPlayFabId = PlayFabManager.Instance.GetPlayFabId();
        if (string.IsNullOrEmpty(currentUserPlayFabId))
        {
            Debug.LogWarning("[BranchInit] Cannot process referral - no PlayFab ID");
            return;
        }
        
        // Don't process if user referred themselves
        if (currentUserPlayFabId == inviterPlayFabId)
        {
            Debug.Log("[BranchInit] User cannot refer themselves - skipping referral");
            return;
        }
        
        Debug.Log($"[BranchInit] Processing referral: {currentUserPlayFabId} was referred by {inviterPlayFabId}");
        
        // Save referral to PlayFab
        PlayFabManager.Instance.SaveReferralData(inviterPlayFabId, inviterName, (success) =>
        {
            if (success)
            {
                Debug.Log("[BranchInit] ✅ Referral data saved to PlayFab successfully!");
                
                // Clear pending referral flags
                PlayerPrefs.DeleteKey("PendingReferrerPlayFabId");
                PlayerPrefs.DeleteKey("PendingReferrerName");
                PlayerPrefs.DeleteKey("HasPendingReferral");
                PlayerPrefs.Save();
            }
            else
            {
                Debug.LogError("[BranchInit] ❌ Failed to save referral data to PlayFab");
            }
        });
    }
    
    /// <summary>
    /// Check for pending referral after PlayFab login
    /// Call this from PlayFabManager after successful login
    /// </summary>
    public void CheckPendingReferral()
    {
        if (PlayerPrefs.GetInt("HasPendingReferral", 0) == 1)
        {
            string inviterPlayFabId = PlayerPrefs.GetString("PendingReferrerPlayFabId", "");
            string inviterName = PlayerPrefs.GetString("PendingReferrerName", "");
            
            if (!string.IsNullOrEmpty(inviterPlayFabId))
            {
                Debug.Log("[BranchInit] Found pending referral, processing now...");
                ProcessReferralAfterLogin(inviterPlayFabId, inviterName);
            }
        }
    }

    /// <summary>
    /// Create and share an invite link when user clicks invite button
    /// Call this method from your invite button's onClick event
    /// </summary>
    public void CreateAndShareInviteLink()
    {
        if (!isBranchInitialized)
        {
            Debug.LogError("[BranchInit] ❌ Cannot create invite link - Branch not initialized yet!");
            return;
        }

        Debug.Log("[BranchInit] Creating invite link...");

        try
        {
            // Create Branch Universal Object for the invite
            BranchUniversalObject universalObject = new BranchUniversalObject();
            universalObject.canonicalIdentifier = "invite_friend";
            universalObject.title = "Join Dice Rush!";
            universalObject.contentDescription = "Play Dice Rush with me! Use my invite link to get started.";
            universalObject.contentIndexMode = 0;
            universalObject.localIndexMode = 0;

            // Add custom metadata
            universalObject.metadata.AddCustomMetadata("invite_type", "friend_invite");
            universalObject.metadata.AddCustomMetadata("app_name", "Dice Rush");
            
            // Create link properties first
            BranchLinkProperties linkProperties = new BranchLinkProperties();
            
            // Add current user's PlayFab ID and name to the invite link
            if (PlayFabManager.Instance != null && PlayFabManager.Instance.IsLoggedIn())
            {
                string inviterPlayFabId = PlayFabManager.Instance.GetPlayFabId();
                string inviterName = PlayFabManager.Instance.currentPlayerData != null ? 
                    PlayFabManager.Instance.currentPlayerData.playerName : "Player";
                
                universalObject.metadata.AddCustomMetadata("inviter_playfab_id", inviterPlayFabId);
                universalObject.metadata.AddCustomMetadata("inviter_name", inviterName);
                
                // Also add to control params so it's easily accessible
                linkProperties.controlParams.Add("inviter_playfab_id", inviterPlayFabId);
                linkProperties.controlParams.Add("inviter_name", inviterName);
                
                Debug.Log($"[BranchInit] Added referrer info to invite link - ID: {inviterPlayFabId}, Name: {inviterName}");
            }
            else
            {
                Debug.LogWarning("[BranchInit] User not logged into PlayFab - invite link created without referrer info");
            }
            linkProperties.feature = "invite";
            linkProperties.channel = "app_invite";
            linkProperties.tags.Add("friend_invite");
            linkProperties.tags.Add("dice_rush");
            
            // Set control parameters (these will be passed when link is clicked)
            linkProperties.controlParams.Add("invite_type", "friend_invite");
            linkProperties.controlParams.Add("source", "app");
            
            // Desktop fallback URL (if user clicks link on desktop)
            linkProperties.controlParams.Add("$desktop_url", "https://your-website.com/download");

            // Generate short URL
            Debug.Log("[BranchInit] Generating Branch short URL...");
            Branch.getShortURL(universalObject, linkProperties, (url, error) =>
            {
                if (error != null)
                {
                    Debug.LogError("[BranchInit] ❌ Failed to create invite link: " + error);
                    return;
                }

                Debug.Log("[BranchInit] ✅ Invite link created successfully!");
                Debug.Log("[BranchInit] 📎 Invite URL: " + url);

                // Share the link using Branch's share functionality
                ShareInviteLink(universalObject, linkProperties, url);
            });
        }
        catch (Exception e)
        {
            Debug.LogError("[BranchInit] ❌ Exception while creating invite link: " + e.Message);
        }
    }

    /// <summary>
    /// Share the invite link using native share dialog
    /// </summary>
    void ShareInviteLink(BranchUniversalObject universalObject, BranchLinkProperties linkProperties, string url)
    {
        Debug.Log("[BranchInit] Sharing invite link...");
        
        string shareMessage = "Join me in Dice Rush! Use my invite link: " + url;
        
        // Use Branch's shareLink method which handles native sharing
        Branch.shareLink(universalObject, linkProperties, shareMessage, (parameters, error) =>
        {
            if (error != null)
            {
                Debug.LogError("[BranchInit] ❌ Failed to share invite link: " + error);
            }
            else
            {
                Debug.Log("[BranchInit] ✅ Invite link shared successfully!");
                Debug.Log("[BranchInit] Share completed with parameters:");
                if (parameters != null)
                {
                    foreach (var param in parameters)
                    {
                        Debug.Log($"[BranchInit]   {param.Key}: {param.Value}");
                    }
                }
            }
        });
    }

    /// <summary>
    /// Alternative method: Just get the URL without sharing (if you want custom UI)
    /// </summary>
    public void GetInviteLinkURL(Action<string> onLinkReady)
    {
        if (!isBranchInitialized)
        {
            Debug.LogError("[BranchInit] ❌ Cannot get invite link - Branch not initialized!");
            onLinkReady?.Invoke(null);
            return;
        }

        BranchUniversalObject universalObject = new BranchUniversalObject();
        universalObject.canonicalIdentifier = "invite_friend";
        universalObject.title = "Join Dice Rush!";
        universalObject.contentDescription = "Play Dice Rush with me!";

        BranchLinkProperties linkProperties = new BranchLinkProperties();
        linkProperties.feature = "invite";
        linkProperties.channel = "app_invite";
        linkProperties.controlParams.Add("invite_type", "friend_invite");

        Branch.getShortURL(universalObject, linkProperties, (url, error) =>
        {
            if (error != null)
            {
                Debug.LogError("[BranchInit] Failed to get invite URL: " + error);
                onLinkReady?.Invoke(null);
            }
            else
            {
                Debug.Log("[BranchInit] Invite URL ready: " + url);
                onLinkReady?.Invoke(url);
            }
        });
    }
}
