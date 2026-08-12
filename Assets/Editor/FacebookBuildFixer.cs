using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// Bypasses Facebook SDK's strict OpenSSL/keytool check on Windows Editor during Android builds.
[InitializeOnLoad]
public class FacebookBuildFixer : IPreprocessBuildWithReport
{
    public int callbackOrder => -1000;

    static FacebookBuildFixer()
    {
        BypassFacebookKeyHashCheck();
    }

    public void OnPreprocessBuild(BuildReport report)
    {
        BypassFacebookKeyHashCheck();
    }

    public static void BypassFacebookKeyHashCheck()
    {
        try
        {
            Type fbAndroidUtil = Type.GetType("Facebook.Unity.Editor.FacebookAndroidUtil, Facebook.Unity.Editor");
            if (fbAndroidUtil != null)
            {
                FieldInfo keyHashField = fbAndroidUtil.GetField("debugKeyHash", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                if (keyHashField != null)
                {
                    string currentVal = keyHashField.GetValue(null) as string;
                    if (string.IsNullOrEmpty(currentVal))
                    {
                        keyHashField.SetValue(null, "BypassedKeyHashForAndroidBuild");
                        Debug.Log("[FacebookBuildFixer] Bypassed Facebook Android debug key hash check for build.");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[FacebookBuildFixer] Failed to bypass Facebook debugKeyHash: " + ex.Message);
        }
    }
}
