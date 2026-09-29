#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class AutoAttachScriptsTool
{
    [MenuItem("Tools/Attach Avatar Scripts")]
    public static void AttachScripts()
    {
        // 1. Find AvatarSelector and attach script
        GameObject avatarSelectorObj = GameObject.Find("AvatarSelector");
        if (avatarSelectorObj != null)
        {
            if (avatarSelectorObj.GetComponent<AvatarSelector>() == null)
            {
                avatarSelectorObj.AddComponent<AvatarSelector>();
                Debug.Log("✅ AvatarSelector script successfully attached to AvatarSelector GameObject.");
            }
            else
            {
                Debug.Log("AvatarSelector script is already attached.");
            }
            Selection.activeGameObject = avatarSelectorObj; // Highlight it for you
        }
        else
        {
            Debug.LogWarning("❌ Could not find a GameObject named 'AvatarSelector' in the scene.");
        }
    }
}
#endif
