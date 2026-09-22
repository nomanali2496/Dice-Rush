using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

public class FixFramesTool
{
    [MenuItem("Tools/Fix Frame Images")]
    public static void FixFrames()
    {
        FixComponent<MainMenuAvatarDisplay>("avatarImage", "frameImageSprite");
        FixComponent<WaitingPlayerSlotUI>("playerAvatar", "frameImage");
        FixComponent<WinnerPlayerRowUI>("avatarImage", "frameImage");
        FixComponent<PlayerProfileUI>("profileImageSprite", "profileFrameDisplay");
        FixComponent<ProfilePanelUI>("profileIcon", "frameIcon");
        FixComponent<LeaderboardRowUI>("avatarImage", "profileBackground");

        AssetDatabase.SaveAssets();
        Debug.Log("<color=green>Finished fixing all frame images! You can now test it.</color>");
    }

    private static void FixComponent<T>(string avatarFieldName, string frameFieldName) where T : MonoBehaviour
    {
        // Find in scene
        T[] inScene = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (T comp in inScene)
        {
            FixSingle(comp, avatarFieldName, frameFieldName, false);
        }

        // Find in prefabs
        string[] guids = AssetDatabase.FindAssets("t:Prefab");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                T[] comps = prefab.GetComponentsInChildren<T>(true);
                foreach (T comp in comps)
                {
                    FixSingle(comp, avatarFieldName, frameFieldName, true);
                    EditorUtility.SetDirty(prefab);
                }
            }
        }
    }

    private static void FixSingle<T>(T comp, string avatarFieldName, string frameFieldName, bool isPrefab) where T : MonoBehaviour
    {
        SerializedObject so = new SerializedObject(comp);
        SerializedProperty avatarProp = so.FindProperty(avatarFieldName);
        SerializedProperty frameProp = so.FindProperty(frameFieldName);

        if (avatarProp == null || frameProp == null) return;

        Image avatarImg = avatarProp.objectReferenceValue as Image;
        if (avatarImg == null) return;

        Image frameImg = frameProp.objectReferenceValue as Image;
        if (frameImg == null)
        {
            // Frame is missing, create it
            GameObject frameObj = new GameObject("FrameImage");
            frameObj.transform.SetParent(avatarImg.transform.parent, false);
            
            // Re-order to be sibling right after the avatar (so it renders on top)
            frameObj.transform.SetSiblingIndex(avatarImg.transform.GetSiblingIndex() + 1);

            Image newImage = frameObj.AddComponent<Image>();
            newImage.raycastTarget = false; // Don't block clicks
            
            // Match rect transform to avatar
            RectTransform rt = frameObj.GetComponent<RectTransform>();
            RectTransform avatarRt = avatarImg.GetComponent<RectTransform>();
            
            rt.anchorMin = avatarRt.anchorMin;
            rt.anchorMax = avatarRt.anchorMax;
            rt.pivot = avatarRt.pivot;
            rt.anchoredPosition = avatarRt.anchoredPosition;
            rt.sizeDelta = avatarRt.sizeDelta * 1.15f; // Make frame slightly larger than the avatar

            frameProp.objectReferenceValue = newImage;
            so.ApplyModifiedProperties();

            Debug.Log($"Fixed {typeof(T).Name} on {comp.gameObject.name} ({(isPrefab ? "Prefab" : "Scene")})");
        }
    }
}
